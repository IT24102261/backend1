using System.Net;
using System.Text.Json;
using FixFlow.Application.DTOs.Maps;
using FixFlow.Application.Interfaces;
using FixFlow.Application.Maps;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FixFlow.Infrastructure.ExternalServices;

public sealed class HttpMapService(
    HttpClient http,
    IOptions<MapOptions> options,
    ILogger<HttpMapService> logger) : IMapService
{
    public async Task<GeocodeResult> GeocodeAddressAsync(string address, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(address))
        {
            return new GeocodeResult { Found = false, ErrorCode = "INVALID_ADDRESS" };
        }

        var document = await SendAsync(
            $"search?q={Uri.EscapeDataString(address.Trim())}&format=json&limit=1",
            cancellationToken);
        if (document is null)
        {
            return new GeocodeResult { Found = false, ErrorCode = "PROVIDER_UNAVAILABLE" };
        }

        if (document.RootElement.ValueKind != JsonValueKind.Array || document.RootElement.GetArrayLength() == 0)
        {
            return new GeocodeResult { Found = false, ErrorCode = "INVALID_ADDRESS" };
        }

        var first = document.RootElement[0];
        if (!TryCoordinate(first, "lat", out var lat) || !TryCoordinate(first, "lon", out var lon))
        {
            return new GeocodeResult { Found = false, ErrorCode = "INVALID_ADDRESS" };
        }

        return new GeocodeResult
        {
            Found = true,
            Latitude = lat,
            Longitude = lon,
            DisplayName = first.TryGetProperty("display_name", out var name) ? name.GetString() : address.Trim()
        };
    }

    public async Task<ReverseGeocodeResult> ReverseGeocodeAsync(double latitude, double longitude, CancellationToken cancellationToken = default)
    {
        if (!IsFinite(latitude) || !IsFinite(longitude))
        {
            return new ReverseGeocodeResult { Found = false, ErrorCode = "INVALID_ADDRESS" };
        }

        var document = await SendAsync(
            $"reverse?lat={latitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}&lon={longitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}&format=json",
            cancellationToken);
        if (document is null)
        {
            return new ReverseGeocodeResult { Found = false, ErrorCode = "PROVIDER_UNAVAILABLE" };
        }

        if (document.RootElement.ValueKind != JsonValueKind.Object
            || !document.RootElement.TryGetProperty("display_name", out var name)
            || string.IsNullOrWhiteSpace(name.GetString()))
        {
            return new ReverseGeocodeResult { Found = false, ErrorCode = "INVALID_ADDRESS" };
        }

        string? area = null;
        if (document.RootElement.TryGetProperty("address", out var address) && address.ValueKind == JsonValueKind.Object)
        {
            area = ReadFirst(address, "city", "town", "village", "state", "county");
        }

        return new ReverseGeocodeResult
        {
            Found = true,
            DisplayName = name.GetString(),
            ServiceArea = area
        };
    }

    public async Task<DistanceResult> CalculateApproximateDistanceAsync(
        MapPoint origin,
        MapPoint destination,
        CancellationToken cancellationToken = default)
    {
        var from = await ResolveAsync(origin, cancellationToken);
        var to = await ResolveAsync(destination, cancellationToken);
        if (from is null || to is null)
        {
            return DistanceResult.Unavailable(from is null || to is null ? "DISTANCE_UNAVAILABLE" : null);
        }

        var km = MapGeometry.ApproximateDistanceKm(from.Value.Lat, from.Value.Lng, to.Value.Lat, to.Value.Lng);
        return new DistanceResult
        {
            DistanceUnavailable = false,
            DistanceKm = km,
            Band = MapGeometry.Band(km)
        };
    }

    private async Task<(double Lat, double Lng)?> ResolveAsync(MapPoint point, CancellationToken cancellationToken)
    {
        if (point.Latitude is double lat && point.Longitude is double lng && IsFinite(lat) && IsFinite(lng))
        {
            return (lat, lng);
        }

        var query = string.IsNullOrWhiteSpace(point.Address) ? point.ServiceArea : point.Address;
        if (string.IsNullOrWhiteSpace(query))
        {
            return null;
        }

        var geocoded = await GeocodeAddressAsync(query, cancellationToken);
        return geocoded.Found && geocoded.Latitude is double gLat && geocoded.Longitude is double gLng
            ? (gLat, gLng)
            : null;
    }

    private async Task<JsonDocument?> SendAsync(string relativeUrl, CancellationToken cancellationToken)
    {
        var settings = options.Value;
        if (string.Equals(settings.Provider, "Disabled", StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrWhiteSpace(settings.BaseUrl))
        {
            logger.LogWarning("Map provider is disabled. Returning unavailable.");
            return null;
        }

        Exception? last = null;
        var attempts = Math.Max(1, settings.MaxRetries + 1);
        for (var attempt = 1; attempt <= attempts; attempt++)
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, relativeUrl);
                if (!string.IsNullOrWhiteSpace(settings.ApiKey))
                {
                    request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {settings.ApiKey}");
                }

                using var response = await http.SendAsync(request, cancellationToken);
                if (response.StatusCode == HttpStatusCode.TooManyRequests || (int)response.StatusCode >= 500)
                {
                    logger.LogWarning("Map provider returned {Status} on attempt {Attempt}", (int)response.StatusCode, attempt);
                    last = new HttpRequestException($"Map provider status {(int)response.StatusCode}");
                    await Delay(settings, attempt, cancellationToken);
                    continue;
                }

                if (!response.IsSuccessStatusCode)
                {
                    logger.LogWarning("Map provider returned {Status} for {Path}", (int)response.StatusCode, relativeUrl);
                    return null;
                }

                var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                return await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                logger.LogWarning("Map provider timed out on attempt {Attempt}", attempt);
                last = new TimeoutException("Map provider timed out.");
                await Delay(settings, attempt, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Map provider call failed on attempt {Attempt}", attempt);
                last = ex;
                await Delay(settings, attempt, cancellationToken);
            }
        }

        logger.LogError(last, "Map provider unavailable after retries. Location will not be invented.");
        return null;
    }

    private static async Task Delay(MapOptions settings, int attempt, CancellationToken cancellationToken)
    {
        if (attempt <= 0)
        {
            return;
        }

        var delay = Math.Max(50, settings.RetryDelayMilliseconds) * attempt;
        try
        {
            await Task.Delay(delay, cancellationToken);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
        }
    }

    private static bool TryCoordinate(JsonElement element, string name, out double value)
    {
        value = 0;
        if (!element.TryGetProperty(name, out var property))
        {
            return false;
        }

        if (property.ValueKind == JsonValueKind.Number)
        {
            return property.TryGetDouble(out value) && IsFinite(value);
        }

        return double.TryParse(property.GetString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out value)
            && IsFinite(value);
    }

    private static string? ReadFirst(JsonElement element, params string[] names)
    {
        foreach (var name in names)
        {
            if (element.TryGetProperty(name, out var property) && property.ValueKind == JsonValueKind.String)
            {
                var value = property.GetString();
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value;
                }
            }
        }

        return null;
    }

    private static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
}

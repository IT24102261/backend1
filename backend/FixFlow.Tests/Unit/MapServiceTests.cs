using System.Net;
using FixFlow.Application.DTOs.Maps;
using FixFlow.Application.Maps;
using FixFlow.Infrastructure.ExternalServices;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace FixFlow.Tests.Unit;

public class MapServiceTests
{
    [Fact]
    public void SuccessfulDistanceCalculation_UsesHaversineWithoutInventing()
    {
        var km = MapGeometry.ApproximateDistanceKm(9.6615, 80.0255, 9.6680, 80.0255);
        Assert.InRange(km, 0.5, 1.5);
        Assert.Equal("LOCAL", MapGeometry.Band(km));
        Assert.Equal("NEAR", MapGeometry.Band(12));
        Assert.Equal("FAR", MapGeometry.Band(40));
    }

    [Fact]
    public async Task CalculateApproximateDistance_WhenCoordinatesExist_DoesNotCallProvider()
    {
        var maps = CreateService(new StubHandler(_ => throw new InvalidOperationException("Provider should not be called.")));
        var result = await maps.CalculateApproximateDistanceAsync(
            new MapPoint { Latitude = 9.6615, Longitude = 80.0255 },
            new MapPoint { Latitude = 9.6680, Longitude = 80.0255 });

        Assert.False(result.DistanceUnavailable);
        Assert.NotNull(result.DistanceKm);
        Assert.Equal("LOCAL", result.Band);
    }

    [Fact]
    public async Task InvalidAddress_DoesNotInventCoordinates()
    {
        var maps = CreateService(new StubHandler(_ => json("[]")));
        var result = await maps.GeocodeAddressAsync("not-a-real-place-zzz-999");

        Assert.False(result.Found);
        Assert.Null(result.Latitude);
        Assert.Null(result.Longitude);
        Assert.Equal("INVALID_ADDRESS", result.ErrorCode);
    }

    [Fact]
    public async Task ProviderUnavailable_ReturnsDistanceUnavailable()
    {
        var maps = CreateService(new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)), retries: 0);
        var geocode = await maps.GeocodeAddressAsync("Jaffna");
        var distance = await maps.CalculateApproximateDistanceAsync(
            new MapPoint { Address = "Jaffna" },
            new MapPoint { Address = "Colombo" });

        Assert.False(geocode.Found);
        Assert.Equal("PROVIDER_UNAVAILABLE", geocode.ErrorCode);
        Assert.True(distance.DistanceUnavailable);
        Assert.Null(distance.DistanceKm);
        Assert.Null(distance.Band);
    }

    [Fact]
    public async Task Timeout_ReturnsUnavailableWithoutInventing()
    {
        var maps = CreateService(new DelayHandler(TimeSpan.FromSeconds(3)), timeoutSeconds: 1, retries: 0);
        var result = await maps.GeocodeAddressAsync("Jaffna");

        Assert.False(result.Found);
        Assert.Null(result.Latitude);
        Assert.True(result.ErrorCode is "PROVIDER_UNAVAILABLE" or "TIMEOUT");
    }

    [Fact]
    public async Task EmptyAddress_IsInvalid()
    {
        var maps = CreateService(new StubHandler(_ => throw new InvalidOperationException("Should not call provider.")));
        var result = await maps.GeocodeAddressAsync("  ");
        Assert.False(result.Found);
        Assert.Equal("INVALID_ADDRESS", result.ErrorCode);
    }

    private static HttpMapService CreateService(HttpMessageHandler handler, int timeoutSeconds = 5, int retries = 1)
    {
        var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://maps.test/"),
            Timeout = TimeSpan.FromSeconds(timeoutSeconds)
        };
        var options = Options.Create(new MapOptions
        {
            Provider = "Nominatim",
            BaseUrl = "https://maps.test/",
            TimeoutSeconds = timeoutSeconds,
            MaxRetries = retries,
            RetryDelayMilliseconds = 10
        });
        return new HttpMapService(client, options, NullLogger<HttpMapService>.Instance);
    }

    private static HttpResponseMessage json(string body) =>
        new(HttpStatusCode.OK) { Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json") };

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(responder(request));
    }

    private sealed class DelayHandler(TimeSpan delay) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            await Task.Delay(delay, cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK);
        }
    }
}

using FixFlow.Application.DTOs.Maps;
using FixFlow.Application.Interfaces;
using FixFlow.Application.Maps;

namespace FixFlow.Tests.Support;

public sealed class CoordinateOnlyMapService : IMapService
{
    public Task<GeocodeResult> GeocodeAddressAsync(string address, CancellationToken cancellationToken = default) =>
        Task.FromResult(new GeocodeResult { Found = false, ErrorCode = "DISTANCE_UNAVAILABLE" });

    public Task<ReverseGeocodeResult> ReverseGeocodeAsync(double latitude, double longitude, CancellationToken cancellationToken = default) =>
        Task.FromResult(new ReverseGeocodeResult { Found = false, ErrorCode = "DISTANCE_UNAVAILABLE" });

    public Task<DistanceResult> CalculateApproximateDistanceAsync(
        MapPoint origin,
        MapPoint destination,
        CancellationToken cancellationToken = default)
    {
        if (origin.Latitude is double lat1 && origin.Longitude is double lng1
            && destination.Latitude is double lat2 && destination.Longitude is double lng2)
        {
            var km = MapGeometry.ApproximateDistanceKm(lat1, lng1, lat2, lng2);
            return Task.FromResult(new DistanceResult
            {
                DistanceUnavailable = false,
                DistanceKm = km,
                Band = MapGeometry.Band(km)
            });
        }

        return Task.FromResult(DistanceResult.Unavailable());
    }
}

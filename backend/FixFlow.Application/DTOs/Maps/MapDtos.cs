namespace FixFlow.Application.DTOs.Maps;

public class MapPoint
{
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? Address { get; set; }
    public string? ServiceArea { get; set; }
}

public class GeocodeResult
{
    public bool Found { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? DisplayName { get; set; }
    public string? ErrorCode { get; set; }
}

public class ReverseGeocodeResult
{
    public bool Found { get; set; }
    public string? DisplayName { get; set; }
    public string? ServiceArea { get; set; }
    public string? ErrorCode { get; set; }
}

public class DistanceResult
{
    public bool DistanceUnavailable { get; set; } = true;
    public double? DistanceKm { get; set; }
    public string? Band { get; set; }
    public string? ErrorCode { get; set; }

    public static DistanceResult Unavailable(string? errorCode = "DISTANCE_UNAVAILABLE") => new()
    {
        DistanceUnavailable = true,
        ErrorCode = errorCode
    };
}

public class GeocodeAddressRequest
{
    public string Address { get; set; } = string.Empty;
}

public class ReverseGeocodeRequest
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}

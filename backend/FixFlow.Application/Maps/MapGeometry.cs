namespace FixFlow.Application.Maps;

public static class MapGeometry
{
    public const double LocalKm = 8;
    public const double NearKm = 25;

    public static double ApproximateDistanceKm(double latitude1, double longitude1, double latitude2, double longitude2)
    {
        const double earthKm = 6371;
        var lat1 = DegreesToRadians(latitude1);
        var lat2 = DegreesToRadians(latitude2);
        var dLat = DegreesToRadians(latitude2 - latitude1);
        var dLon = DegreesToRadians(longitude2 - longitude1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
            + Math.Cos(lat1) * Math.Cos(lat2) * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return Math.Round(earthKm * c, 1);
    }

    public static string Band(double distanceKm) =>
        distanceKm <= LocalKm ? "LOCAL" : distanceKm <= NearKm ? "NEAR" : "FAR";

    private static double DegreesToRadians(double degrees) => degrees * Math.PI / 180;
}

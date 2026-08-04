using BaseStationReader.Interfaces.Geometry;

namespace BaseStationReader.BusinessLogic.Geometry;

/// <summary>
/// Performs renderer-neutral geographic calculations on a spherical Earth model.
/// </summary>
public sealed class GeographicCalculator : IGeographicCalculator
{
    private const double EarthRadiusMetres = 6371000d;

    /// <inheritdoc />
    public bool IsValidCoordinate(double latitude, double longitude)
    {
        // Both values must be finite and lie within conventional geographic bounds.
        return double.IsFinite(latitude) && latitude is >= -90d and <= 90d &&
               double.IsFinite(longitude) && longitude is >= -180d and <= 180d;
    }

    /// <inheritdoc />
    public double CalculateInitialBearing(
        double fromLatitude,
        double fromLongitude,
        double toLatitude,
        double toLongitude)
    {
        ValidateCoordinate(fromLatitude, fromLongitude, "origin");
        ValidateCoordinate(toLatitude, toLongitude, "destination");

        // Apply the standard initial great-circle bearing formula in radians.
        var fromLatitudeRadians = ToRadians(fromLatitude);
        var toLatitudeRadians = ToRadians(toLatitude);
        var longitudeDifference = ToRadians(toLongitude - fromLongitude);
        var y = Math.Sin(longitudeDifference) * Math.Cos(toLatitudeRadians);
        var x = Math.Cos(fromLatitudeRadians) * Math.Sin(toLatitudeRadians) -
                Math.Sin(fromLatitudeRadians) * Math.Cos(toLatitudeRadians) * Math.Cos(longitudeDifference);

        // Normalise atan2's signed result into the conventional zero-to-360-degree range.
        return (ToDegrees(Math.Atan2(y, x)) + 360d) % 360d;
    }

    /// <inheritdoc />
    public double CalculateAngularDistance(
        double fromLatitude,
        double fromLongitude,
        double toLatitude,
        double toLongitude)
    {
        ValidateCoordinate(fromLatitude, fromLongitude, "origin");
        ValidateCoordinate(toLatitude, toLongitude, "destination");

        // Unit-vector dot products provide a stable central angle across the antimeridian.
        var start = ToUnitVector(fromLatitude, fromLongitude);
        var end = ToUnitVector(toLatitude, toLongitude);
        var dot = Math.Clamp(start.X * end.X + start.Y * end.Y + start.Z * end.Z, -1d, 1d);
        return Math.Acos(dot);
    }

    /// <inheritdoc />
    public (double Latitude, double Longitude) InterpolateGreatCircle(
        double fromLatitude,
        double fromLongitude,
        double toLatitude,
        double toLongitude,
        double fraction)
    {
        ValidateCoordinate(fromLatitude, fromLongitude, "origin");
        ValidateCoordinate(toLatitude, toLongitude, "destination");
        if (!double.IsFinite(fraction) || fraction is < 0d or > 1d)
            throw new ArgumentOutOfRangeException(nameof(fraction), "Interpolation fraction must be between zero and one.");

        var start = ToUnitVector(fromLatitude, fromLongitude);
        var end = ToUnitVector(toLatitude, toLongitude);
        var angle = CalculateAngularDistance(fromLatitude, fromLongitude, toLatitude, toLongitude);
        if (angle < 1e-10d) return (fromLatitude, fromLongitude);

        // Spherical linear interpolation follows the shortest surface route between the endpoints.
        var denominator = Math.Sin(angle);
        var startWeight = Math.Sin((1d - fraction) * angle) / denominator;
        var endWeight = Math.Sin(fraction * angle) / denominator;
        var x = startWeight * start.X + endWeight * end.X;
        var y = startWeight * start.Y + endWeight * end.Y;
        var z = startWeight * start.Z + endWeight * end.Z;
        return (
            ToDegrees(Math.Atan2(z, Math.Sqrt(x * x + y * y))),
            ToDegrees(Math.Atan2(y, x)));
    }

    /// <inheritdoc />
    public (double EastMetres, double NorthMetres) ProjectToLocalMetres(
        double originLatitude,
        double originLongitude,
        double latitude,
        double longitude)
    {
        ValidateCoordinate(originLatitude, originLongitude, "origin");
        ValidateCoordinate(latitude, longitude, "position");

        // Use a local equirectangular projection centred on the supplied origin.
        var originLatitudeRadians = ToRadians(originLatitude);
        var east = (ToRadians(longitude) - ToRadians(originLongitude)) *
                   Math.Cos(originLatitudeRadians) * EarthRadiusMetres;
        var north = (ToRadians(latitude) - originLatitudeRadians) * EarthRadiusMetres;
        return (east, north);
    }

    /// <summary>Throws when a coordinate cannot participate in a geographic calculation.</summary>
    private void ValidateCoordinate(double latitude, double longitude, string parameterName)
    {
        // Centralise validation so every calculation applies identical coordinate semantics.
        if (!IsValidCoordinate(latitude, longitude))
            throw new ArgumentOutOfRangeException(parameterName, "Latitude must be between -90 and 90 and longitude between -180 and 180.");
    }

    /// <summary>Converts a geographic coordinate into a Cartesian unit vector.</summary>
    private static (double X, double Y, double Z) ToUnitVector(double latitude, double longitude)
    {
        // Convert degrees before applying the standard spherical-to-Cartesian transform.
        var latitudeRadians = ToRadians(latitude);
        var longitudeRadians = ToRadians(longitude);
        var latitudeCosine = Math.Cos(latitudeRadians);
        return (
            latitudeCosine * Math.Cos(longitudeRadians),
            latitudeCosine * Math.Sin(longitudeRadians),
            Math.Sin(latitudeRadians));
    }

    /// <summary>Converts degrees to radians.</summary>
    private static double ToRadians(double degrees) => degrees * Math.PI / 180d;

    /// <summary>Converts radians to degrees.</summary>
    private static double ToDegrees(double radians) => radians * 180d / Math.PI;
}

namespace BaseStationReader.Interfaces.Geometry;

/// <summary>
/// Provides consistent spherical geographic validation and calculations.
/// </summary>
public interface IGeographicCalculator
{
    /// <summary>Returns whether a latitude and longitude form a finite geographic coordinate.</summary>
    bool IsValidCoordinate(double latitude, double longitude);

    /// <summary>Calculates the initial great-circle bearing from one coordinate to another.</summary>
    double CalculateInitialBearing(
        double fromLatitude,
        double fromLongitude,
        double toLatitude,
        double toLongitude);

    /// <summary>Calculates the central angle in radians between two coordinates.</summary>
    double CalculateAngularDistance(
        double fromLatitude,
        double fromLongitude,
        double toLatitude,
        double toLongitude);

    /// <summary>Calculates the great-circle distance in metres between two coordinates.</summary>
    double CalculateDistanceMetres(
        double fromLatitude,
        double fromLongitude,
        double toLatitude,
        double toLongitude);

    /// <summary>Calculates a destination coordinate from a start, initial bearing, and distance.</summary>
    (double Latitude, double Longitude) CalculateDestinationPoint(
        double latitude,
        double longitude,
        double bearing,
        double distanceMetres);

    /// <summary>Interpolates a coordinate along the shortest great-circle route.</summary>
    (double Latitude, double Longitude) InterpolateGreatCircle(
        double fromLatitude,
        double fromLongitude,
        double toLatitude,
        double toLongitude,
        double fraction);

    /// <summary>Projects a coordinate into local east/north metre offsets from an origin.</summary>
    (double EastMetres, double NorthMetres) ProjectToLocalMetres(
        double originLatitude,
        double originLongitude,
        double latitude,
        double longitude);
}

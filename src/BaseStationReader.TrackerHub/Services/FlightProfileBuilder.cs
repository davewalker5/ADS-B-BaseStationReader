using BaseStationReader.TrackerHub.Models;

namespace BaseStationReader.TrackerHub.Services;

/// <summary>
/// Produces deterministic chart-ready flight profiles from persisted position projections.
/// </summary>
public sealed class FlightProfileBuilder : IFlightProfileBuilder
{
    private readonly double? _receiverLatitude;
    private readonly double? _receiverLongitude;

    /// <summary>
    /// Initialises profile preparation with the configured receiver position.
    /// </summary>
    /// <param name="receiverLatitude">Receiver latitude in degrees.</param>
    /// <param name="receiverLongitude">Receiver longitude in degrees.</param>
    public FlightProfileBuilder(double? receiverLatitude, double? receiverLongitude)
    {
        // Bearing enrichment is optional because older configurations may omit receiver coordinates.
        _receiverLatitude = receiverLatitude;
        _receiverLongitude = receiverLongitude;
    }

    /// <inheritdoc />
    public FlightProfileDto Build(
        int trackingRecordId,
        string address,
        string callsign,
        IEnumerable<FlightProfilePointDto> points)
    {
        ArgumentNullException.ThrowIfNull(points);

        // Stable timestamp ordering preserves the recorded path when multiple points share a timestamp.
        var ordered = points
            .Select((point, sourceIndex) => new { Point = point, SourceIndex = sourceIndex })
            .OrderBy(item => item.Point.Timestamp)
            .ThenBy(item => item.SourceIndex)
            .Select((item, sequence) => new FlightProfilePointDto
            {
                Sequence = sequence + 1,
                Timestamp = item.Point.Timestamp,
                Latitude = item.Point.Latitude,
                Longitude = item.Point.Longitude,
                Altitude = item.Point.Altitude,
                Distance = item.Point.Distance,
                Bearing = CalculateBearing(item.Point.Latitude, item.Point.Longitude)
            })
            .ToArray();

        var altitudePoints = ordered.Where(point => point.Altitude.HasValue).ToArray();
        var distancePoints = ordered.Where(point => point.Distance.HasValue).ToArray();

        // Summaries deliberately ignore missing telemetry rather than treating it as zero.
        return new FlightProfileDto
        {
            TrackingRecordId = trackingRecordId,
            Address = address,
            Callsign = callsign,
            Points = ordered,
            FirstTimestamp = ordered.FirstOrDefault()?.Timestamp,
            FinalTimestamp = ordered.LastOrDefault()?.Timestamp,
            InitialAltitude = altitudePoints.FirstOrDefault()?.Altitude,
            FinalAltitude = altitudePoints.LastOrDefault()?.Altitude,
            MinimumAltitude = altitudePoints.Select(point => point.Altitude).Min(),
            MaximumAltitude = altitudePoints.Select(point => point.Altitude).Max(),
            ClosestDistance = distancePoints.Select(point => point.Distance).Min(),
            FurthestDistance = distancePoints.Select(point => point.Distance).Max()
        };
    }

    /// <summary>
    /// Calculates the initial bearing from the configured receiver to a position.
    /// </summary>
    /// <param name="latitude">Position latitude in degrees.</param>
    /// <param name="longitude">Position longitude in degrees.</param>
    /// <returns>Bearing in degrees, or <see langword="null"/> when coordinates are unavailable.</returns>
    private double? CalculateBearing(decimal? latitude, decimal? longitude)
    {
        // All four coordinates are required for a meaningful great-circle bearing.
        if (!_receiverLatitude.HasValue || !_receiverLongitude.HasValue || !latitude.HasValue || !longitude.HasValue)
        {
            return null;
        }

        var receiverLatitude = DegreesToRadians(_receiverLatitude.Value);
        var positionLatitude = DegreesToRadians((double)latitude.Value);
        var longitudeDifference = DegreesToRadians((double)longitude.Value - _receiverLongitude.Value);
        var y = Math.Sin(longitudeDifference) * Math.Cos(positionLatitude);
        var x = Math.Cos(receiverLatitude) * Math.Sin(positionLatitude) -
                Math.Sin(receiverLatitude) * Math.Cos(positionLatitude) * Math.Cos(longitudeDifference);

        // Normalise atan2's signed result into the conventional 0–360 degree bearing range.
        return (RadiansToDegrees(Math.Atan2(y, x)) + 360) % 360;
    }

    /// <summary>
    /// Converts an angle from degrees to radians.
    /// </summary>
    /// <param name="degrees">Angle in degrees.</param>
    /// <returns>The angle in radians.</returns>
    private static double DegreesToRadians(double degrees)
    {
        // Trigonometric functions in the base class library consume radians.
        return degrees * Math.PI / 180;
    }

    /// <summary>
    /// Converts an angle from radians to degrees.
    /// </summary>
    /// <param name="radians">Angle in radians.</param>
    /// <returns>The angle in degrees.</returns>
    private static double RadiansToDegrees(double radians)
    {
        // Convert the atan2 result back into a value suitable for display.
        return radians * 180 / Math.PI;
    }
}

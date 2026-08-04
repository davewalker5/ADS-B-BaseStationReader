using BaseStationReader.Entities.History;
using BaseStationReader.Interfaces.Geometry;
using BaseStationReader.Interfaces.Tracking;

namespace BaseStationReader.BusinessLogic.Tracking;

/// <summary>
/// Produces deterministic, renderer-neutral flight profiles from persisted position projections.
/// </summary>
public sealed class FlightProfileBuilder : IFlightProfileBuilder
{
    private readonly Func<(double? Latitude, double? Longitude)> _receiverPosition;
    private readonly IGeographicCalculator _geographicCalculator;

    /// <summary>
    /// Initialises profile preparation with fixed receiver coordinates.
    /// </summary>
    /// <param name="receiverLatitude">Receiver latitude in degrees.</param>
    /// <param name="receiverLongitude">Receiver longitude in degrees.</param>
    public FlightProfileBuilder(
        double? receiverLatitude,
        double? receiverLongitude,
        IGeographicCalculator geographicCalculator)
    {
        ArgumentNullException.ThrowIfNull(geographicCalculator);

        // Bearing enrichment is optional because older configurations may omit receiver coordinates.
        _receiverPosition = () => (receiverLatitude, receiverLongitude);
        _geographicCalculator = geographicCalculator;
    }

    /// <summary>
    /// Initialises profile preparation with the current receiver-position provider.
    /// </summary>
    /// <param name="receiverPositionProvider">Provides coordinates that can change with the active tracking profile.</param>
    public FlightProfileBuilder(
        IReceiverPositionProvider receiverPositionProvider,
        IGeographicCalculator geographicCalculator)
    {
        ArgumentNullException.ThrowIfNull(receiverPositionProvider);
        ArgumentNullException.ThrowIfNull(geographicCalculator);

        // Resolve the position for each build so a newly applied tracking profile is observed immediately.
        _receiverPosition = () => receiverPositionProvider.ReceiverPosition;
        _geographicCalculator = geographicCalculator;
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

        // Summaries deliberately ignore missing telemetry rather than treating it as zero.
        var altitudePoints = ordered.Where(point => point.Altitude.HasValue).ToArray();
        var distancePoints = ordered.Where(point => point.Distance.HasValue).ToArray();

        // Return only entity-layer DTOs so any presentation or reporting implementation can consume the result.
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
        var receiverPosition = _receiverPosition();
        if (!receiverPosition.Latitude.HasValue || !receiverPosition.Longitude.HasValue ||
            !latitude.HasValue || !longitude.HasValue ||
            !_geographicCalculator.IsValidCoordinate(receiverPosition.Latitude.Value, receiverPosition.Longitude.Value) ||
            !_geographicCalculator.IsValidCoordinate((double)latitude.Value, (double)longitude.Value))
        {
            return null;
        }

        // Delegate bearing semantics to the shared geographic calculator.
        return _geographicCalculator.CalculateInitialBearing(
            receiverPosition.Latitude.Value,
            receiverPosition.Longitude.Value,
            (double)latitude.Value,
            (double)longitude.Value);
    }
}

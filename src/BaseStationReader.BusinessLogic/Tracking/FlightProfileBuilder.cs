using BaseStationReader.Entities.History;
using BaseStationReader.Interfaces.Geometry;
using BaseStationReader.Interfaces.Tracking;

namespace BaseStationReader.BusinessLogic.Tracking;

/// <summary>
/// Produces deterministic, renderer-neutral flight profiles from persisted position projections.
/// </summary>
public sealed class FlightProfileBuilder : IFlightProfileBuilder
{
    private const double FeetToMetres = 0.3048;
    private const double MaximumPlausibleGroundSpeedMetresPerSecond = 500;
    private const double MaximumPlausibleVerticalSpeedMetresPerSecond = 100;
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
    public FlightProfile Build(
        int trackingRecordId,
        string address,
        string callsign,
        IEnumerable<FlightProfilePoint> points)
    {
        ArgumentNullException.ThrowIfNull(points);

        // Both profile charts require a real timestamp and altitude. Coordinates and distance remain optional because
        // the time chart can still represent a useful profile without them.
        var ordered = points
            .Where(point => point.Timestamp != default && point.Altitude.HasValue)
            .Select((point, sourceIndex) => new { Point = point, SourceIndex = sourceIndex })
            .OrderBy(item => item.Point.Timestamp)
            .ThenBy(item => item.SourceIndex)
            .Select(item => item.Point)
            .ToArray();

        // Apply the same isolated-spike protections as the flight-path page before calculating chart summaries.
        ordered = DiscardImplausiblePositionSpikes(ordered);
        ordered = DiscardImplausibleAltitudeSpikes(ordered);

        var prepared = ordered
            .Select((point, sequence) => new FlightProfilePoint
            {
                Sequence = sequence + 1,
                Timestamp = point.Timestamp,
                Latitude = point.Latitude,
                Longitude = point.Longitude,
                Altitude = point.Altitude,
                Distance = point.Distance is double distance && double.IsFinite(distance) ? distance : null,
                Bearing = CalculateBearing(point.Latitude, point.Longitude)
            })
            .ToArray();

        // Summaries deliberately ignore missing telemetry rather than treating it as zero.
        var altitudePoints = prepared.Where(point => point.Altitude.HasValue).ToArray();
        var distancePoints = prepared.Where(point => point.Distance.HasValue).ToArray();

        // Return only entity-layer DTOs so any presentation or reporting implementation can consume the result.
        return new FlightProfile
        {
            TrackingRecordId = trackingRecordId,
            Address = address,
            Callsign = callsign,
            Points = prepared,
            FirstTimestamp = prepared.FirstOrDefault()?.Timestamp,
            FinalTimestamp = prepared.LastOrDefault()?.Timestamp,
            InitialAltitude = altitudePoints.FirstOrDefault()?.Altitude,
            FinalAltitude = altitudePoints.LastOrDefault()?.Altitude,
            MinimumAltitude = altitudePoints.Select(point => point.Altitude).Min(),
            MaximumAltitude = altitudePoints.Select(point => point.Altitude).Max(),
            ClosestDistance = distancePoints.Select(point => point.Distance).Min(),
            FurthestDistance = distancePoints.Select(point => point.Distance).Max()
        };
    }

    /// <summary>Discards isolated impossible geographic excursions when all three coordinates are available.</summary>
    private FlightProfilePoint[] DiscardImplausiblePositionSpikes(IReadOnlyList<FlightProfilePoint> points)
    {
        if (points.Count < 3)
        {
            return points.ToArray();
        }

        var rejected = new HashSet<int>();
        for (var index = 1; index < points.Count - 1; index++)
        {
            var previous = points[index - 1];
            var current = points[index];
            var following = points[index + 1];
            if (!HasValidCoordinate(previous) || !HasValidCoordinate(current) || !HasValidCoordinate(following))
            {
                continue;
            }

            var secondsBefore = (current.Timestamp - previous.Timestamp).TotalSeconds;
            var secondsAfter = (following.Timestamp - current.Timestamp).TotalSeconds;
            var secondsAcross = (following.Timestamp - previous.Timestamp).TotalSeconds;
            if (Math.Min(secondsBefore, Math.Min(secondsAfter, secondsAcross)) <= 0)
            {
                continue;
            }

            var speedOut = DistanceMetres(previous, current) / secondsBefore;
            var speedBack = DistanceMetres(current, following) / secondsAfter;
            var speedAcross = DistanceMetres(previous, following) / secondsAcross;
            if (speedOut > MaximumPlausibleGroundSpeedMetresPerSecond &&
                speedBack > MaximumPlausibleGroundSpeedMetresPerSecond &&
                speedAcross <= MaximumPlausibleGroundSpeedMetresPerSecond)
            {
                rejected.Add(index);
            }
        }

        return points.Where((_, index) => !rejected.Contains(index)).ToArray();
    }

    /// <summary>Discards an isolated altitude that requires an impossible vertical jump away and back.</summary>
    private static FlightProfilePoint[] DiscardImplausibleAltitudeSpikes(IReadOnlyList<FlightProfilePoint> points)
    {
        if (points.Count < 3)
        {
            return points.ToArray();
        }

        var rejected = new HashSet<int>();
        for (var index = 1; index < points.Count - 1; index++)
        {
            var previous = points[index - 1];
            var current = points[index];
            var following = points[index + 1];
            var secondsBefore = (current.Timestamp - previous.Timestamp).TotalSeconds;
            var secondsAfter = (following.Timestamp - current.Timestamp).TotalSeconds;
            var secondsAcross = (following.Timestamp - previous.Timestamp).TotalSeconds;
            if (Math.Min(secondsBefore, Math.Min(secondsAfter, secondsAcross)) <= 0)
            {
                continue;
            }

            var speedOut = Math.Abs((double)(current.Altitude!.Value - previous.Altitude!.Value)) * FeetToMetres / secondsBefore;
            var speedBack = Math.Abs((double)(following.Altitude!.Value - current.Altitude!.Value)) * FeetToMetres / secondsAfter;
            var speedAcross = Math.Abs((double)(following.Altitude!.Value - previous.Altitude!.Value)) * FeetToMetres / secondsAcross;
            if (speedOut > MaximumPlausibleVerticalSpeedMetresPerSecond &&
                speedBack > MaximumPlausibleVerticalSpeedMetresPerSecond &&
                speedAcross <= MaximumPlausibleVerticalSpeedMetresPerSecond)
            {
                rejected.Add(index);
            }
        }

        return points.Where((_, index) => !rejected.Contains(index)).ToArray();
    }

    /// <summary>Returns whether a profile point can participate in geographic spike detection.</summary>
    private bool HasValidCoordinate(FlightProfilePoint point) =>
        point.Latitude.HasValue && point.Longitude.HasValue &&
        _geographicCalculator.IsValidCoordinate((double)point.Latitude.Value, (double)point.Longitude.Value);

    /// <summary>Calculates great-circle distance between two points with validated coordinates.</summary>
    private double DistanceMetres(FlightProfilePoint first, FlightProfilePoint second) =>
        _geographicCalculator.CalculateDistanceMetres(
            (double)first.Latitude!.Value,
            (double)first.Longitude!.Value,
            (double)second.Latitude!.Value,
            (double)second.Longitude!.Value);

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

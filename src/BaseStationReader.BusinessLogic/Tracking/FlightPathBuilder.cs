using BaseStationReader.Entities.History;
using BaseStationReader.Interfaces.Geometry;
using BaseStationReader.Interfaces.Tracking;

namespace BaseStationReader.BusinessLogic.Tracking;

/// <summary>
/// Reproduces the repeatable data transformations from the flight-path notebook in C#.
/// </summary>
public sealed class FlightPathBuilder : IFlightPathBuilder
{
    private const double FeetToMetres = 0.3048;
    private const double BoundingBoxPaddingRatio = 0.06;
    private const double MaximumPlausibleGroundSpeedMetresPerSecond = 500;
    private const double MaximumPlausibleVerticalSpeedMetresPerSecond = 100;
    private static readonly TimeSpan MaximumSegmentGap = TimeSpan.FromSeconds(90);
    private readonly Func<(double? Latitude, double? Longitude)> _receiverPosition;
    private readonly IGeographicCalculator _geographicCalculator;

    /// <summary>
    /// Initialises flight-path preparation with optional receiver coordinates.
    /// </summary>
    /// <param name="receiverLatitude">Receiver latitude in degrees.</param>
    /// <param name="receiverLongitude">Receiver longitude in degrees.</param>
    public FlightPathBuilder(
        double? receiverLatitude,
        double? receiverLongitude,
        IGeographicCalculator geographicCalculator)
    {
        ArgumentNullException.ThrowIfNull(geographicCalculator);
        // Receiver coordinates enrich hover data but are not required for path projection.
        _receiverPosition = () => (receiverLatitude, receiverLongitude);
        _geographicCalculator = geographicCalculator;
    }

    /// <summary>
    /// Initialises flight-path preparation with the current receiver-position provider.
    /// </summary>
    /// <param name="receiverPositionProvider">Provides coordinates that can change with the active tracking profile.</param>
    public FlightPathBuilder(
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
    public FlightPath Build(
        int trackingRecordId,
        string address,
        string callsign,
        string registration,
        string model,
        string flightNumber,
        string airline,
        string route,
        IEnumerable<FlightProfilePoint> points)
    {
        ArgumentNullException.ThrowIfNull(points);

        // Match the notebook by requiring timestamp, position, altitude, and receiver distance.
        var validPoints = points
            .Where(IsValid)
            .OrderBy(point => point.Timestamp)
            .GroupBy(point => new
            {
                point.Timestamp,
                Latitude = point.Latitude!.Value,
                Longitude = point.Longitude!.Value,
                Altitude = point.Altitude!.Value,
                Distance = point.Distance!.Value
            })
            .Select(group => group.First())
            .ToArray();

        // Remove only isolated telemetry spikes. Sustained receiver catch-up changes remain valid because they do not
        // immediately return to the preceding trajectory or altitude.
        validPoints = DiscardImplausiblePositionSpikes(validPoints);
        validPoints = DiscardImplausibleAltitudeSpikes(validPoints);

        if (validPoints.Length == 0)
        {
            return EmptyPath(trackingRecordId, address, callsign, registration, model, flightNumber, airline, route);
        }

        // Use the first valid observation as the origin for the local metric projection.
        var referenceLatitude = (double)validPoints[0].Latitude!.Value;
        var referenceLongitude = (double)validPoints[0].Longitude!.Value;
        var segment = 1;
        DateTime? previousTimestamp = null;
        var prepared = new List<FlightPathPoint>(validPoints.Length);

        foreach (var point in validPoints)
        {
            // Break the ribbon rather than drawing a misleading connector across long reception gaps.
            if (previousTimestamp.HasValue && point.Timestamp - previousTimestamp.Value > MaximumSegmentGap)
            {
                segment++;
            }

            var latitude = (double)point.Latitude!.Value;
            var longitude = (double)point.Longitude!.Value;
            var localPosition = _geographicCalculator.ProjectToLocalMetres(
                referenceLatitude, referenceLongitude, latitude, longitude);
            prepared.Add(new FlightPathPoint
            {
                Sequence = prepared.Count + 1,
                Segment = segment,
                Timestamp = point.Timestamp,
                Latitude = latitude,
                Longitude = longitude,
                AltitudeFeet = (double)point.Altitude!.Value,
                AltitudeMetres = (double)point.Altitude.Value * FeetToMetres,
                DistanceNauticalMiles = point.Distance!.Value,
                BearingDegrees = CalculateBearing(latitude, longitude),
                LocalXMetres = localPosition.EastMetres,
                LocalYMetres = localPosition.NorthMetres
            });
            previousTimestamp = point.Timestamp;
        }

        // Add stable padding so flat or very short paths still produce useful renderer bounds.
        var altitudes = prepared.Select(point => point.AltitudeMetres).ToArray();
        var altitudePadding = Math.Max((altitudes.Max() - altitudes.Min()) * 0.1, 250);
        var minimumAltitude = Math.Max(0, altitudes.Min() - altitudePadding);
        var maximumAltitude = altitudes.Max() + altitudePadding;
        var latitudes = prepared.Select(point => point.Latitude).ToArray();
        var longitudes = prepared.Select(point => point.Longitude).ToArray();
        var latitudePadding = Math.Max(latitudes.Max() - latitudes.Min(), 0.000001) * BoundingBoxPaddingRatio;
        var longitudePadding = Math.Max(longitudes.Max() - longitudes.Min(), 0.000001) * BoundingBoxPaddingRatio;

        // Return only entity-layer DTO values so any presentation or reporting implementation can consume the result.
        var receiverPosition = _receiverPosition();
        return new FlightPath
        {
            TrackingRecordId = trackingRecordId,
            Address = address,
            Callsign = callsign,
            Registration = registration,
            Model = model,
            FlightNumber = flightNumber,
            Airline = airline,
            Route = route,
            Points = prepared,
            North = latitudes.Max() + latitudePadding,
            South = latitudes.Min() - latitudePadding,
            East = longitudes.Max() + longitudePadding,
            West = longitudes.Min() - longitudePadding,
            ReceiverLatitude = receiverPosition.Latitude,
            ReceiverLongitude = receiverPosition.Longitude,
            MinimumAltitudeMetres = minimumAltitude,
            MaximumAltitudeMetres = maximumAltitude,
            SegmentCount = segment
        };
    }

    /// <summary>
    /// Determines whether a raw point contains every value required by the notebook plot.
    /// </summary>
    /// <param name="point">Raw persisted position projection.</param>
    /// <returns><see langword="true"/> when the point can be plotted.</returns>
    private bool IsValid(FlightProfilePoint point)
    {
        // Reject invalid geographic ranges as well as incomplete nullable telemetry.
        return point.Timestamp != default && point.Latitude.HasValue && point.Longitude.HasValue &&
               _geographicCalculator.IsValidCoordinate((double)point.Latitude.Value, (double)point.Longitude.Value) &&
               point.Altitude.HasValue &&
               point.Distance.HasValue;
    }

    /// <summary>Discards an isolated coordinate that requires an impossible jump away from and back to the path.</summary>
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

    /// <summary>Calculates great-circle distance between two already validated raw points.</summary>
    private double DistanceMetres(FlightProfilePoint first, FlightProfilePoint second) =>
        _geographicCalculator.CalculateDistanceMetres(
            (double)first.Latitude!.Value,
            (double)first.Longitude!.Value,
            (double)second.Latitude!.Value,
            (double)second.Longitude!.Value);

    /// <summary>
    /// Creates a metadata-preserving result when no positions can be plotted.
    /// </summary>
    /// <param name="trackingRecordId">Owning tracking-record identifier.</param>
    /// <param name="address">Aircraft ICAO address.</param>
    /// <param name="callsign">Aircraft callsign.</param>
    /// <param name="registration">Aircraft registration.</param>
    /// <param name="model">Aircraft model.</param>
    /// <param name="flightNumber">Associated flight number.</param>
    /// <param name="airline">Associated airline.</param>
    /// <param name="route">Associated route.</param>
    /// <returns>An empty path retaining the supplied flight metadata.</returns>
    private static FlightPath EmptyPath(
        int trackingRecordId,
        string address,
        string callsign,
        string registration,
        string model,
        string flightNumber,
        string airline,
        string route)
    {
        // Keeping metadata lets the UI explain which record lacks sufficient coordinates.
        return new FlightPath
        {
            TrackingRecordId = trackingRecordId,
            Address = address,
            Callsign = callsign,
            Registration = registration,
            Model = model,
            FlightNumber = flightNumber,
            Airline = airline,
            Route = route
        };
    }

    /// <summary>
    /// Calculates initial bearing from the configured receiver to a path position.
    /// </summary>
    /// <param name="latitude">Position latitude in degrees.</param>
    /// <param name="longitude">Position longitude in degrees.</param>
    /// <returns>Bearing clockwise from true north, or zero when receiver coordinates are unavailable.</returns>
    private double CalculateBearing(double latitude, double longitude)
    {
        // Use zero only when receiver coordinates are absent, matching a non-enriched DTO value.
        var receiverPosition = _receiverPosition();
        if (!receiverPosition.Latitude.HasValue || !receiverPosition.Longitude.HasValue)
        {
            return 0;
        }

        // Apply the initial great-circle bearing formula using radians.
        if (!_geographicCalculator.IsValidCoordinate(receiverPosition.Latitude.Value, receiverPosition.Longitude.Value))
        {
            return 0;
        }

        // Delegate bearing semantics to the shared geographic calculator.
        return _geographicCalculator.CalculateInitialBearing(
            receiverPosition.Latitude.Value, receiverPosition.Longitude.Value, latitude, longitude);
    }
}

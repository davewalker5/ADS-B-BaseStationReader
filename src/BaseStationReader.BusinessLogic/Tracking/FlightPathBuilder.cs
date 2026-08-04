using BaseStationReader.Entities.History;
using BaseStationReader.Interfaces.Tracking;

namespace BaseStationReader.BusinessLogic.Tracking;

/// <summary>
/// Reproduces the repeatable data transformations from the flight-path notebook in C#.
/// </summary>
public sealed class FlightPathBuilder : IFlightPathBuilder
{
    private const double EarthRadiusMetres = 6371000;
    private const double FeetToMetres = 0.3048;
    private const double BoundingBoxPaddingRatio = 0.06;
    private static readonly TimeSpan MaximumSegmentGap = TimeSpan.FromSeconds(90);
    private readonly Func<(double? Latitude, double? Longitude)> _receiverPosition;

    /// <summary>
    /// Initialises flight-path preparation with optional receiver coordinates.
    /// </summary>
    /// <param name="receiverLatitude">Receiver latitude in degrees.</param>
    /// <param name="receiverLongitude">Receiver longitude in degrees.</param>
    public FlightPathBuilder(double? receiverLatitude, double? receiverLongitude)
    {
        // Receiver coordinates enrich hover data but are not required for path projection.
        _receiverPosition = () => (receiverLatitude, receiverLongitude);
    }

    /// <summary>
    /// Initialises flight-path preparation with the current receiver-position provider.
    /// </summary>
    /// <param name="receiverPositionProvider">Provides coordinates that can change with the active tracking profile.</param>
    public FlightPathBuilder(IReceiverPositionProvider receiverPositionProvider)
    {
        ArgumentNullException.ThrowIfNull(receiverPositionProvider);

        // Resolve the position for each build so a newly applied tracking profile is observed immediately.
        _receiverPosition = () => receiverPositionProvider.ReceiverPosition;
    }

    /// <inheritdoc />
    public FlightPathDto Build(
        int trackingRecordId,
        string address,
        string callsign,
        string registration,
        string model,
        string flightNumber,
        string airline,
        string route,
        IEnumerable<FlightProfilePointDto> points)
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

        if (validPoints.Length == 0)
        {
            return EmptyPath(trackingRecordId, address, callsign, registration, model, flightNumber, airline, route);
        }

        // Use the first valid observation as the origin for the local metric projection.
        var referenceLatitudeRadians = DegreesToRadians((double)validPoints[0].Latitude!.Value);
        var referenceLongitudeRadians = DegreesToRadians((double)validPoints[0].Longitude!.Value);
        var segment = 1;
        DateTime? previousTimestamp = null;
        var prepared = new List<FlightPathPointDto>(validPoints.Length);

        foreach (var point in validPoints)
        {
            // Break the ribbon rather than drawing a misleading connector across long reception gaps.
            if (previousTimestamp.HasValue && point.Timestamp - previousTimestamp.Value > MaximumSegmentGap)
            {
                segment++;
            }

            var latitude = (double)point.Latitude!.Value;
            var longitude = (double)point.Longitude!.Value;
            var latitudeRadians = DegreesToRadians(latitude);
            var longitudeRadians = DegreesToRadians(longitude);
            prepared.Add(new FlightPathPointDto
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
                LocalXMetres = (longitudeRadians - referenceLongitudeRadians) * Math.Cos(referenceLatitudeRadians) * EarthRadiusMetres,
                LocalYMetres = (latitudeRadians - referenceLatitudeRadians) * EarthRadiusMetres
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
        return new FlightPathDto
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
    private static bool IsValid(FlightProfilePointDto point)
    {
        // Reject invalid geographic ranges as well as incomplete nullable telemetry.
        return point.Timestamp != default &&
               point.Latitude is >= -90 and <= 90 &&
               point.Longitude is >= -180 and <= 180 &&
               point.Altitude.HasValue &&
               point.Distance.HasValue;
    }

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
    private static FlightPathDto EmptyPath(
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
        return new FlightPathDto
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
        var receiverLatitude = DegreesToRadians(receiverPosition.Latitude.Value);
        var positionLatitude = DegreesToRadians(latitude);
        var longitudeDifference = DegreesToRadians(longitude - receiverPosition.Longitude.Value);
        var y = Math.Sin(longitudeDifference) * Math.Cos(positionLatitude);
        var x = Math.Cos(receiverLatitude) * Math.Sin(positionLatitude) -
                Math.Sin(receiverLatitude) * Math.Cos(positionLatitude) * Math.Cos(longitudeDifference);

        // Normalise the signed atan2 result into a compass bearing.
        return (RadiansToDegrees(Math.Atan2(y, x)) + 360) % 360;
    }

    /// <summary>
    /// Converts degrees to radians for projection and bearing calculations.
    /// </summary>
    /// <param name="degrees">Angle in degrees.</param>
    /// <returns>The equivalent angle in radians.</returns>
    private static double DegreesToRadians(double degrees)
    {
        // Base trigonometric functions operate on radians.
        return degrees * Math.PI / 180;
    }

    /// <summary>
    /// Converts radians to degrees for the renderer-neutral DTO.
    /// </summary>
    /// <param name="radians">Angle in radians.</param>
    /// <returns>The equivalent angle in degrees.</returns>
    private static double RadiansToDegrees(double radians)
    {
        // Convert the calculated bearing into conventional display-independent degrees.
        return radians * 180 / Math.PI;
    }
}

#nullable enable

using BaseStationReader.Entities.Hub;
using BaseStationReader.TrackerHub.Models;

namespace BaseStationReader.TrackerHub.Services;

/// <summary>
/// Calculates bearing and normalised screen coordinates for live radar targets.
/// </summary>
public sealed class RadarProjectionService : IRadarProjectionService
{
    private readonly Func<(double? Latitude, double? Longitude)> _receiverPosition;

    /// <summary>
    /// Initialises radar projection around the configured receiver.
    /// </summary>
    /// <param name="receiverLatitude">Receiver latitude in degrees.</param>
    /// <param name="receiverLongitude">Receiver longitude in degrees.</param>
    public RadarProjectionService(double? receiverLatitude, double? receiverLongitude)
    {
        _receiverPosition = () => (receiverLatitude, receiverLongitude);
    }

    public RadarProjectionService(IReceiverPositionProvider receiverPositionProvider)
        => _receiverPosition = () => receiverPositionProvider.ReceiverPosition;

    /// <inheritdoc />
    public RadarPointDto? Project(TrackedAircraftDto aircraft, double maximumRange)
    {
        var receiverPosition = _receiverPosition();
        if (maximumRange <= 0 || aircraft.Distance is null || aircraft.Latitude is null || aircraft.Longitude is null ||
            receiverPosition.Latitude is null || receiverPosition.Longitude is null)
        {
            // Distance and both endpoints are required to place a target reliably.
            return null;
        }

        var bearing = CalculateBearing(
            receiverPosition.Latitude.Value,
            receiverPosition.Longitude.Value,
            (double)aircraft.Latitude.Value,
            (double)aircraft.Longitude.Value);
        var coordinates = ProjectCoordinates(aircraft.Distance.Value, bearing, maximumRange);
        if (coordinates is null)
        {
            // The earlier validation normally prevents this fallback, but keep projection independently safe.
            return null;
        }

        return new RadarPointDto
        {
            Address = aircraft.Address,
            Label = string.IsNullOrWhiteSpace(aircraft.Callsign) ? aircraft.Address : aircraft.Callsign.Trim(),
            Distance = aircraft.Distance.Value,
            Bearing = bearing,
            X = coordinates.Value.X,
            Y = coordinates.Value.Y,
            Altitude = aircraft.Altitude,
            Track = aircraft.Track,
            Status = aircraft.Status,
            Timestamp = aircraft.LastSeen
        };
    }

    /// <inheritdoc />
    public (double X, double Y)? ProjectCoordinates(double distance, double bearing, double maximumRange)
    {
        if (maximumRange <= 0 || !double.IsFinite(distance) || !double.IsFinite(bearing))
        {
            // Invalid scale or telemetry cannot produce meaningful screen coordinates.
            return null;
        }

        var angle = bearing * Math.PI / 180d;
        var radius = distance / maximumRange;

        // SVG Y increases downwards, so north is represented by a negative Y value.
        return (radius * Math.Sin(angle), -radius * Math.Cos(angle));
    }

    /// <summary>
    /// Calculates the initial great-circle bearing between two coordinates.
    /// </summary>
    /// <param name="fromLatitude">Starting latitude in degrees.</param>
    /// <param name="fromLongitude">Starting longitude in degrees.</param>
    /// <param name="toLatitude">Destination latitude in degrees.</param>
    /// <param name="toLongitude">Destination longitude in degrees.</param>
    /// <returns>Bearing clockwise from true north in the range zero to 360 degrees.</returns>
    private static double CalculateBearing(double fromLatitude, double fromLongitude, double toLatitude, double toLongitude)
    {
        var fromLat = DegreesToRadians(fromLatitude);
        var toLat = DegreesToRadians(toLatitude);
        var deltaLongitude = DegreesToRadians(toLongitude - fromLongitude);

        // Use the standard initial-bearing formula so projection remains correct over realistic receiver ranges.
        var y = Math.Sin(deltaLongitude) * Math.Cos(toLat);
        var x = Math.Cos(fromLat) * Math.Sin(toLat) - Math.Sin(fromLat) * Math.Cos(toLat) * Math.Cos(deltaLongitude);
        return (Math.Atan2(y, x) * 180d / Math.PI + 360d) % 360d;
    }

    /// <summary>
    /// Converts degrees to radians for trigonometric calculations.
    /// </summary>
    /// <param name="degrees">Angle in degrees.</param>
    /// <returns>The equivalent angle in radians.</returns>
    private static double DegreesToRadians(double degrees)
    {
        // Keeping conversion local makes the bearing calculation explicit and independently testable.
        return degrees * Math.PI / 180d;
    }
}

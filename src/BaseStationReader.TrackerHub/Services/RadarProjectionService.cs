#nullable enable

using BaseStationReader.Entities.Hub;
using BaseStationReader.Interfaces.Geometry;
using BaseStationReader.Interfaces.Tracking;
using BaseStationReader.TrackerHub.Models;

namespace BaseStationReader.TrackerHub.Services;

/// <summary>
/// Calculates bearing and normalised screen coordinates for live radar targets.
/// </summary>
public sealed class RadarProjectionService : IRadarProjectionService
{
    private readonly Func<(double? Latitude, double? Longitude)> _receiverPosition;
    private readonly IGeographicCalculator _geographicCalculator;

    /// <summary>
    /// Initialises radar projection around the configured receiver.
    /// </summary>
    /// <param name="receiverLatitude">Receiver latitude in degrees.</param>
    /// <param name="receiverLongitude">Receiver longitude in degrees.</param>
    public RadarProjectionService(
        double? receiverLatitude,
        double? receiverLongitude,
        IGeographicCalculator geographicCalculator)
    {
        ArgumentNullException.ThrowIfNull(geographicCalculator);

        _receiverPosition = () => (receiverLatitude, receiverLongitude);
        _geographicCalculator = geographicCalculator;
    }

    /// <summary>
    /// Initialises radar projection with a dynamic receiver-position provider.
    /// </summary>
    public RadarProjectionService(
        IReceiverPositionProvider receiverPositionProvider,
        IGeographicCalculator geographicCalculator)
    {
        ArgumentNullException.ThrowIfNull(receiverPositionProvider);
        ArgumentNullException.ThrowIfNull(geographicCalculator);

        // Resolve receiver coordinates for each projection so active profile changes are honoured.
        _receiverPosition = () => receiverPositionProvider.ReceiverPosition;
        _geographicCalculator = geographicCalculator;
    }

    /// <inheritdoc />
    public RadarPointDto? Project(TrackedAircraftDto aircraft, double maximumRange)
    {
        var receiverPosition = _receiverPosition();
        if (maximumRange <= 0 || aircraft.Distance is null || aircraft.Latitude is null || aircraft.Longitude is null ||
            receiverPosition.Latitude is null || receiverPosition.Longitude is null ||
            !_geographicCalculator.IsValidCoordinate(receiverPosition.Latitude.Value, receiverPosition.Longitude.Value) ||
            !_geographicCalculator.IsValidCoordinate((double)aircraft.Latitude.Value, (double)aircraft.Longitude.Value))
        {
            // Distance and both endpoints are required to place a target reliably.
            return null;
        }

        var bearing = _geographicCalculator.CalculateInitialBearing(
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

}

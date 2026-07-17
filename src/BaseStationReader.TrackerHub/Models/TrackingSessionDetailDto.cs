#nullable enable

using BaseStationReader.Entities.Tracking;

namespace BaseStationReader.TrackerHub.Models;

/// <summary>
/// Contains the read-only historical detail for one tracking record.
/// </summary>
public sealed class TrackingSessionDetailDto
{
    public int Id { get; init; }
    public string Address { get; init; } = string.Empty;
    public string Callsign { get; init; } = string.Empty;
    public string Squawk { get; init; } = string.Empty;
    public decimal? Altitude { get; init; }
    public decimal? GroundSpeed { get; init; }
    public decimal? Track { get; init; }
    public decimal? Latitude { get; init; }
    public decimal? Longitude { get; init; }
    public double? Distance { get; init; }
    public decimal? VerticalRate { get; init; }
    public DateTime FirstSeen { get; init; }
    public DateTime LastSeen { get; init; }
    public int Messages { get; init; }
    public TrackingStatus Status { get; init; }
    public int PositionCount { get; init; }
    public decimal? MinimumAltitude { get; init; }
    public decimal? MaximumAltitude { get; init; }
    public double? MinimumDistance { get; init; }
    public double? MaximumDistance { get; init; }
    public PositionSummaryDto? FirstPosition { get; init; }
    public PositionSummaryDto? FinalPosition { get; init; }
    public string Registration { get; init; } = string.Empty;
    public int? Manufactured { get; init; }
    public int? AircraftAge { get; init; }
    public string ModelName { get; init; } = string.Empty;
    public string ModelIcao { get; init; } = string.Empty;
    public string Manufacturer { get; init; } = string.Empty;
    public string FlightIata { get; init; } = string.Empty;
    public string FlightIcao { get; init; } = string.Empty;
    public string Embarkation { get; init; } = string.Empty;
    public string Destination { get; init; } = string.Empty;
    public string AirlineName { get; init; } = string.Empty;
    public TimeSpan Duration => LastSeen - FirstSeen;
}

#nullable enable

namespace BaseStationReader.TrackerHub.Models;

/// <summary>A read-only aggregate of one persisted observation session.</summary>
public sealed class ObservationSessionSummaryDto
{
    public int SessionId { get; init; }
    public DateTime StartedAtUtc { get; init; }
    public DateTime? LastActivity { get; init; }
    public TimeSpan ObservedDuration { get; init; }
    public string ProfileName { get; init; } = string.Empty;
    public string Notes { get; init; } = string.Empty;
    public double? ReceiverLatitude { get; init; }
    public double? ReceiverLongitude { get; init; }
    public int? ReceiverElevation { get; init; }
    public int? MinimumAltitudeLimit { get; init; }
    public int? MaximumAltitudeLimit { get; init; }
    public int? MaximumDistanceLimit { get; init; }
    public string IncludedBehaviours { get; init; } = string.Empty;
    public int ObservationRecords { get; init; }
    public int DistinctAircraft { get; init; }
    public int DistinctCallsigns { get; init; }
    public int AircraftWithPositionHistory { get; init; }
    public int PositionRecords { get; init; }
    public int IdentifiedAircraft { get; init; }
    public int ResolvedFlights { get; init; }
    public int UnidentifiedAircraft { get; init; }
    public double AircraftResolutionPercentage { get; init; }
    public double FlightResolutionPercentage { get; init; }
    public ObservationHighlightDto? LowestAltitude { get; init; }
    public ObservationHighlightDto? HighestAltitude { get; init; }
    public ObservationHighlightDto? FurthestAircraft { get; init; }
    public ObservationHighlightDto? LongestObservedAircraft { get; init; }
}

/// <summary>Identifies the aircraft and value behind a session highlight.</summary>
public sealed class ObservationHighlightDto
{
    public string Address { get; init; } = string.Empty;
    public string Callsign { get; init; } = string.Empty;
    public decimal? Altitude { get; init; }
    public double? Distance { get; init; }
    public TimeSpan? Duration { get; init; }
}

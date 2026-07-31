#nullable enable

namespace BaseStationReader.TrackerHub.Models;

/// <summary>
/// Describes the read-only operational state of the current observation session.
/// </summary>
public sealed record LiveTrackerStatusDto
{
    public bool IsRunning { get; init; }
    public DateTime? StartedAtUtc { get; init; }
    public string? ProfileName { get; init; }
    public string? Notes { get; init; }
    public int CurrentlyTracked { get; init; }
    public long AircraftAdded { get; init; }
    public long AircraftRemoved { get; init; }
    public long PositionRecords { get; init; }
    public long MessagesProcessed { get; init; }
    public long DistinctAircraft { get; init; }
    public long DistinctCallsigns { get; init; }
    public long AircraftWithPositionRecords { get; init; }
    public int AircraftLocallyResolved { get; init; }
    public int AircraftUnresolved { get; init; }
    public int FlightsLocallyResolved { get; init; }
    public int FlightsUnresolved { get; init; }
    public int AircraftWithoutCallsign { get; init; }
    public int AircraftTransientlyResolved { get; init; }
    public int FlightsTransientlyResolved { get; init; }
}

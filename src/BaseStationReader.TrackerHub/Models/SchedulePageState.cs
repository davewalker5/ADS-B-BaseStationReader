using BaseStationReader.Entities.Config;

namespace BaseStationReader.TrackerHub.Models;

/// <summary>
/// Contains the last submitted Schedule-tab criteria for the current in-memory UI session.
/// </summary>
public sealed record SchedulePageState(
    ApiServiceType Service,
    string Iata,
    DateTime From,
    DateTime To);

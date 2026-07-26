#nullable enable

using BaseStationReader.TrackerHub.Models;

namespace BaseStationReader.TrackerHub.Services;

/// <summary>
/// Retains submitted page criteria only for the current TrackerHub UI session.
/// Implementations must not persist values to browser storage, cookies, files, or a database.
/// </summary>
public interface ITrackerHubPageState
{
    LookupPageState? Lookup { get; set; }
    SchedulePageState? Schedule { get; set; }
    WeatherPageState? Weather { get; set; }
}

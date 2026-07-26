#nullable enable

using BaseStationReader.TrackerHub.Models;

namespace BaseStationReader.TrackerHub.Services;

/// <summary>
/// Holds page criteria as object references in one Blazor circuit's process memory.
/// This class has no serializer or storage provider and never writes to browser storage,
/// cookies, a database, or files. Its values disappear when the UI session ends.
/// </summary>
public sealed class TrackerHubPageState : ITrackerHubPageState
{
    public LookupPageState? Lookup { get; set; }
    public SchedulePageState? Schedule { get; set; }
    public WeatherPageState? Weather { get; set; }
}

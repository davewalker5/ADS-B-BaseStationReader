#nullable enable

using BaseStationReader.Entities.Hub;

namespace BaseStationReader.TrackerHub.Models;

/// <summary>
/// Retains the latest completed Live Tracker session within one Blazor UI circuit.
/// </summary>
public sealed class CompletedLiveTrackerPageState
{
    public int SessionId { get; init; }
    public string ActiveTab { get; set; } = "Tracking";
    public double MaximumRadarRange { get; set; }
    public IReadOnlyList<TrackedAircraftDto> Aircraft { get; init; } = [];
}

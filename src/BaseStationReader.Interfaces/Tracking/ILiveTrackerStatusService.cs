#nullable enable

using BaseStationReader.Entities.Hub;
using BaseStationReader.Entities.Tracking;

namespace BaseStationReader.Interfaces.Tracking;

/// <summary>
/// Builds a read-only operational snapshot for the Live Tracker Summary tab.
/// </summary>
public interface ILiveTrackerStatusService
{
    /// <summary>
    /// Builds the status for one observation session from current memory and local reference data.
    /// </summary>
    /// <param name="sessionId">The active or most recently completed session identifier.</param>
    /// <param name="aircraft">The aircraft currently present in the live tracker.</param>
    /// <param name="isRunning">Whether tracking is currently active.</param>
    /// <param name="cancellationToken">Cancels the database reads.</param>
    /// <returns>The operational status snapshot, or <see langword="null"/> before a session exists.</returns>
    Task<LiveTrackerStatusDto?> GetAsync(
        int? sessionId,
        IReadOnlyCollection<TrackedAircraftDto> aircraft,
        bool isRunning,
        CancellationToken cancellationToken = default);
}

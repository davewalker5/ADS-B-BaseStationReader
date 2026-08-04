#nullable enable

using BaseStationReader.Entities.History;
using BaseStationReader.Interfaces.Tracking;

namespace BaseStationReader.BusinessLogic.Tracking;

/// <summary>
/// Maintains the current position-density snapshot in process memory.
/// </summary>
public sealed class PositionDensitySnapshotStateManager : IPositionDensitySnapshotStateManager
{
    private readonly object _sync = new();
    private readonly IPositionDensitySnapshotMerger _merger;
    private PositionDensity? _snapshot;

    /// <summary>
    /// Creates an in-memory snapshot-state manager.
    /// </summary>
    /// <param name="merger"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public PositionDensitySnapshotStateManager(IPositionDensitySnapshotMerger merger)
    {
        ArgumentNullException.ThrowIfNull(merger);
        _merger = merger;
    }

    /// <inheritdoc />
    public PositionDensity? GetSnapshot(int sessionId)
    {
        if (sessionId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sessionId));
        }

        lock (_sync)
        {
            // Never expose a snapshot from a previously selected observation session.
            return _snapshot?.SessionId == sessionId ? _snapshot : null;
        }
    }

    /// <inheritdoc />
    public PositionDensity Merge(PositionDensity refreshed)
    {
        ArgumentNullException.ThrowIfNull(refreshed);

        lock (_sync)
        {
            // Serialise read-merge-write so concurrent UI refreshes cannot lose a newer accumulated value.
            _snapshot = _merger.Merge(_snapshot, refreshed)
                ?? throw new InvalidOperationException("A refreshed density snapshot could not be merged.");
            return _snapshot;
        }
    }

    /// <inheritdoc />
    public void Clear()
    {
        lock (_sync)
        {
            // Clearing releases all state from the previous live-tracker workflow.
            _snapshot = null;
        }
    }
}

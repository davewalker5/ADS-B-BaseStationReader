#nullable enable

using BaseStationReader.Entities.History;

namespace BaseStationReader.Interfaces.Tracking;

/// <summary>
/// Maintains the current position-density snapshot in process memory.
/// </summary>
public interface IPositionDensitySnapshotStateManager
{
    /// <summary>
    /// Gets the current snapshot when it belongs to the requested session.
    /// </summary>
    /// <param name="sessionId"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    PositionDensity? GetSnapshot(int sessionId);

    /// <summary>
    /// Merges a refreshed calculation into the current in-memory snapshot.
    /// </summary>
    /// <param name="refreshed"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    PositionDensity Merge(PositionDensity refreshed);

    /// <summary>
    /// Clears the current in-memory snapshot.
    /// </summary>
    void Clear();
}

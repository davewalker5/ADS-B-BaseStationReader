using BaseStationReader.Entities.History;

#nullable enable

namespace BaseStationReader.Interfaces.Database;

/// <summary>
/// Persists and retrieves immutable position-density snapshots.
/// </summary>
public interface IPositionDensitySnapshotManager
{
    /// <summary>
    /// Adds a complete position-density snapshot atomically.
    /// </summary>
    /// <param name="snapshot"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ArgumentException"></exception>
    Task<int> AddAsync(PositionDensitySnapshotEntity snapshot, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a complete position-density snapshot by identifier.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    Task<PositionDensitySnapshotEntity?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves snapshot metadata for a session in capture order.
    /// </summary>
    /// <param name="sessionId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    Task<IReadOnlyList<PositionDensitySnapshotEntity>> GetForSessionAsync(int sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the latest complete snapshot for a session.
    /// </summary>
    /// <param name="sessionId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    Task<PositionDensitySnapshotEntity?> GetLatestForSessionAsync(int sessionId, CancellationToken cancellationToken = default);
}

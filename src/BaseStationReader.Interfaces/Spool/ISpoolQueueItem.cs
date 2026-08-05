using BaseStationReader.Entities.Spool;

namespace BaseStationReader.Interfaces.Spool;

/// <summary>
/// Represents a transactionally leased spool record.
/// </summary>
public interface ISpoolQueueItem : IDisposable
{
    /// <summary>
    /// Gets the leased record.
    /// </summary>
    SpoolQueueRecord Record { get; }

    /// <summary>
    /// Permanently removes the record from the queue.
    /// </summary>
    void Complete();
}

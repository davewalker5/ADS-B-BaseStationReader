using BaseStationReader.Entities.Spool;

#nullable enable

namespace BaseStationReader.Interfaces.Spool;

/// <summary>
/// Provides persistent FIFO storage for continuous-writer requests.
/// </summary>
public interface ISpoolQueue : IDisposable
{
    /// <summary>
    /// Gets the estimated number of records waiting in the queue.
    /// </summary>
    int Count { get; }

    /// <summary>
    /// Persists a record at the end of the queue.
    /// </summary>
    /// <param name="entity">Entity to persist.</param>
    void Enqueue(object entity);

    /// <summary>
    /// Attempts to lease the record at the head of the queue.
    /// </summary>
    /// <returns>The leased record, or <see langword="null"/> when the queue is empty.</returns>
    ISpoolQueueItem? TryDequeue();
}

using BaseStationReader.BusinessLogic.Spool;
using BaseStationReader.Interfaces.Spool;

#nullable enable

namespace BaseStationReader.Tests.Mocks;

/// <summary>
/// Provides an isolated DiskQueue spool that removes its files after a test.
/// </summary>
internal sealed class TemporarySpoolQueue : ISpoolQueue
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), $"BaseStationReader-{Guid.NewGuid():N}");
    private readonly SpoolQueueManager _queue;

    /// <summary>
    /// Initialises an isolated spool.
    /// </summary>
    public TemporarySpoolQueue()
    {
        _queue = new SpoolQueueManager(_folder);
    }

    /// <inheritdoc />
    public int Count => _queue.Count;

    /// <inheritdoc />
    public void Enqueue(object entity)
        => _queue.Enqueue(entity);

    /// <inheritdoc />
    public void EnqueueRange(IEnumerable<object> entities)
        => _queue.EnqueueRange(entities);

    /// <inheritdoc />
    public ISpoolQueueItem? TryDequeue()
        => _queue.TryDequeue();

    /// <inheritdoc />
    public void Dispose()
    {
        _queue.Dispose();
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, recursive: true);
        }
    }
}

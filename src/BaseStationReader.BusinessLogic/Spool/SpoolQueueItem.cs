using BaseStationReader.Entities.Spool;
using BaseStationReader.Interfaces.Spool;
using DiskQueue;

namespace BaseStationReader.BusinessLogic.Spool;

/// <summary>
/// Owns the DiskQueue transaction associated with one leased record.
/// </summary>
internal sealed class SpoolQueueItem : ISpoolQueueItem
{
    private readonly IPersistentQueueSession _session;
    private bool _disposed;

    /// <summary>
    /// Initialises a leased spool record.
    /// </summary>
    /// <param name="session">Open dequeue transaction.</param>
    /// <param name="record">Deserialized queue record.</param>
    public SpoolQueueItem(
        IPersistentQueueSession session,
        SpoolQueueRecord record)
    {
        _session = session;
        Record = record;
    }

    /// <inheritdoc />
    public SpoolQueueRecord Record { get; }

    /// <inheritdoc />
    public void Complete()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _session.Flush();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _session.Dispose();
        _disposed = true;
    }
}

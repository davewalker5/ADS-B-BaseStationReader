using System.Text.Json;
using System.Text.Json.Serialization;
using BaseStationReader.Entities.History;
using BaseStationReader.Entities.Spool;
using BaseStationReader.Entities.Tracking;
using BaseStationReader.Interfaces.Spool;
using DiskQueue;

#nullable enable

namespace BaseStationReader.BusinessLogic.Spool;

/// <summary>
/// Provides an application-specific persistent FIFO over DiskQueue.
/// </summary>
public sealed class SpoolQueueManager : ISpoolQueue
{
    private readonly IPersistentQueue _queue;
    private readonly JsonSerializerOptions _serializerOptions;

    /// <summary>
    /// Initialises a persistent spool in the specified directory.
    /// </summary>
    /// <param name="spoolFolder">Directory containing the DiskQueue files.</param>
    public SpoolQueueManager(string spoolFolder)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(spoolFolder);

        _serializerOptions = new JsonSerializerOptions();
        _serializerOptions.Converters.Add(new JsonStringEnumConverter());
        _queue = new PersistentQueue(Path.GetFullPath(spoolFolder));
    }

    /// <inheritdoc />
    public int Count => _queue.EstimatedCountOfItemsInQueue;

    /// <inheritdoc />
    public void Enqueue(object entity)
        => EnqueueRange([entity]);

    /// <inheritdoc />
    public void EnqueueRange(IEnumerable<object> entities)
    {
        using var session = _queue.OpenSession();
        foreach (var entity in entities)
        {
            var record = CreateRecord(entity);
            var data = JsonSerializer.SerializeToUtf8Bytes(record, _serializerOptions);
            session.Enqueue(data);
        }
        session.Flush();
    }

    /// <inheritdoc />
    public ISpoolQueueItem? TryDequeue()
    {
        var session = _queue.OpenSession();
        var dequeued = false;

        try
        {
            var data = session.Dequeue();
            if (data is null)
            {
                session.Dispose();
                return null;
            }

            dequeued = true;
            var record = JsonSerializer.Deserialize<SpoolQueueRecord>(data, _serializerOptions)
                ?? throw new InvalidDataException("The spool record contained no data.");
            Validate(record);
            return new SpoolQueueItem(session, record);
        }
        catch
        {
            // An unreadable record can never be processed successfully, so prevent it blocking the FIFO.
            if (dequeued)
            {
                session.Flush();
            }

            session.Dispose();
            throw;
        }
    }

    /// <inheritdoc />
    public void Dispose()
        => _queue.Dispose();

    /// <summary>
    /// Creates a self-contained record without Entity Framework navigation properties.
    /// </summary>
    /// <param name="entity">Entity supplied to the continuous writer.</param>
    /// <returns>Persistable spool record.</returns>
    private static SpoolQueueRecord CreateRecord(object entity)
        => entity switch
        {
            TrackedAircraft aircraft => new SpoolQueueRecord
            {
                Id = Guid.NewGuid(),
                QueuedAtUtc = DateTime.UtcNow,
                EntityType = SpoolEntityType.TrackedAircraft,
                TrackedAircraft = Copy(aircraft)
            },
            AircraftPosition position => new SpoolQueueRecord
            {
                Id = Guid.NewGuid(),
                QueuedAtUtc = DateTime.UtcNow,
                EntityType = SpoolEntityType.AircraftPosition,
                AircraftPosition = Copy(position)
            },
            PositionDensitySnapshotEntity snapshot => new SpoolQueueRecord
            {
                Id = Guid.NewGuid(),
                QueuedAtUtc = DateTime.UtcNow,
                EntityType = SpoolEntityType.PositionDensitySnapshot,
                PositionDensitySnapshot = Copy(snapshot)
            },
            _ => throw new ArgumentException($"Unsupported spool entity type: {entity?.GetType().FullName ?? "null"}.", nameof(entity))
        };

    /// <summary>
    /// Creates a persistence-only aircraft copy.
    /// </summary>
    /// <param name="source">Source aircraft.</param>
    /// <returns>Detached aircraft record.</returns>
    private static TrackedAircraft Copy(TrackedAircraft source)
        => new()
        {
            Id = source.Id,
            SessionId = source.SessionId,
            Address = source.Address,
            Callsign = source.Callsign,
            Squawk = source.Squawk,
            Altitude = source.Altitude,
            GroundSpeed = source.GroundSpeed,
            Track = source.Track,
            Latitude = source.Latitude,
            Longitude = source.Longitude,
            Distance = source.Distance,
            VerticalRate = source.VerticalRate,
            FirstSeen = source.FirstSeen,
            LastSeen = source.LastSeen,
            Messages = source.Messages,
            Status = source.Status
        };

    /// <summary>
    /// Creates a persistence-only position copy.
    /// </summary>
    /// <param name="source">Source position.</param>
    /// <returns>Detached position record.</returns>
    private static AircraftPosition Copy(AircraftPosition source)
        => new()
        {
            Id = source.Id,
            Address = source.Address,
            Altitude = source.Altitude,
            Latitude = source.Latitude,
            Longitude = source.Longitude,
            Distance = source.Distance,
            Timestamp = source.Timestamp,
            AircraftId = source.AircraftId,
            SessionId = source.SessionId
        };

    /// <summary>
    /// Creates a persistence-only snapshot copy.
    /// </summary>
    /// <param name="source">Source snapshot.</param>
    /// <returns>Detached snapshot record.</returns>
    private static PositionDensitySnapshotEntity Copy(PositionDensitySnapshotEntity source)
        => new()
        {
            Id = source.Id,
            SessionId = source.SessionId,
            CapturedAtUtc = source.CapturedAtUtc,
            PositionCount = source.PositionCount,
            MaximumBinCount = source.MaximumBinCount,
            MinimumLatitude = source.MinimumLatitude,
            MaximumLatitude = source.MaximumLatitude,
            MinimumLongitude = source.MinimumLongitude,
            MaximumLongitude = source.MaximumLongitude,
            Cells = source.Cells.Select(cell => new PositionDensitySnapshotCellEntity
            {
                Id = cell.Id,
                PositionDensitySnapshotId = cell.PositionDensitySnapshotId,
                Latitude = cell.Latitude,
                Longitude = cell.Longitude,
                Count = cell.Count
            }).ToList()
        };

    /// <summary>
    /// Verifies a deserialized record contains the payload identified by its discriminator.
    /// </summary>
    /// <param name="record">Record to validate.</param>
    private static void Validate(SpoolQueueRecord record)
    {
        var valid = record.EntityType switch
        {
            SpoolEntityType.TrackedAircraft => record.TrackedAircraft is not null,
            SpoolEntityType.AircraftPosition => record.AircraftPosition is not null,
            SpoolEntityType.PositionDensitySnapshot => record.PositionDensitySnapshot is not null,
            _ => false
        };

        if (!valid)
        {
            throw new InvalidDataException($"Spool record {record.Id} has no valid {record.EntityType} payload.");
        }
    }
}

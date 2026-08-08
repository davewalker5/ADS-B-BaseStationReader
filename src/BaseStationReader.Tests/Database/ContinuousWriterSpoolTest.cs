using BaseStationReader.BusinessLogic.Database;
using BaseStationReader.BusinessLogic.Spool;
using BaseStationReader.Data;
using BaseStationReader.Entities.Tracking;
using BaseStationReader.Interfaces.Database;
using BaseStationReader.Tests.Mocks;

namespace BaseStationReader.Tests.Database;

[TestClass]
public sealed class ContinuousWriterSpoolTest
{
    /// <summary>
    /// Verifies historical records recovered from the durable spool use observation-time continuity.
    /// </summary>
    [TestMethod]
    public async Task ReplayedHistoricalRecordsReuseLifetimeTestAsync()
    {
        var folder = CreateFolder();

        try
        {
            await using var context = BaseStationReaderDbContextFactory.CreateInMemoryDbContext();
            var session = new ObservationSession
            {
                Name = "Replay test",
                StartedAtUtc = DateTime.UtcNow,
                ProfileName = "Test",
                Host = "receiver.local",
                Port = 30003,
                IncludedBehaviours = "Unknown"
            };
            context.ObservationSessions.Add(session);
            await context.SaveChangesAsync();
            var observedAt = DateTime.UtcNow.AddDays(-7);

            using (var seed = new SpoolQueueManager(folder))
            {
                seed.Enqueue(CreateAircraft("406A3D", session.Id, observedAt));
                seed.Enqueue(CreateAircraft("406A3D", session.Id, observedAt.AddMilliseconds(500)));
            }

            IDatabaseManagementFactory factory = new DatabaseManagementFactory(new MockFileLogger(), context, 1000);
            await using var writer = new ContinuousWriter(
                factory,
                new SpoolQueueManager(folder),
                flushOnStop: true);
            await writer.FlushQueueAsync();

            Assert.HasCount(1, context.TrackedAircraft);
        }
        finally
        {
            DeleteFolder(folder);
        }
    }

    /// <summary>
    /// Verifies stopping without flushing leaves recovered work in the spool.
    /// </summary>
    [TestMethod]
    public async Task StopWithoutFlushLeavesPendingRecordsTestAsync()
    {
        var folder = CreateFolder();

        try
        {
            using (var seed = new SpoolQueueManager(folder))
            {
                seed.Enqueue(CreateAircraft());
            }

            await using var context = BaseStationReaderDbContextFactory.CreateInMemoryDbContext();
            IDatabaseManagementFactory factory = new DatabaseManagementFactory(new MockFileLogger(), context, 1000);
            await using (IContinuousWriter writer = new ContinuousWriter(
                factory,
                new SpoolQueueManager(folder),
                flushOnStop: false))
            {
                await writer.StartAsync(new CancellationToken(canceled: true));
                await writer.StopAsync();
            }

            using var reopened = new SpoolQueueManager(folder);
            Assert.AreEqual(1, reopened.Count);
        }
        finally
        {
            DeleteFolder(folder);
        }
    }

    /// <summary>
    /// Verifies stopping with flushing attempts and removes recovered work.
    /// </summary>
    [TestMethod]
    public async Task StopWithFlushDiscardsFailedPendingRecordsTestAsync()
    {
        var folder = CreateFolder();

        try
        {
            using (var seed = new SpoolQueueManager(folder))
            {
                seed.Enqueue(CreateAircraft());
            }

            await using var context = BaseStationReaderDbContextFactory.CreateInMemoryDbContext();
            IDatabaseManagementFactory factory = new DatabaseManagementFactory(new MockFileLogger(), context, 1000);
            await using (IContinuousWriter writer = new ContinuousWriter(
                factory,
                new SpoolQueueManager(folder),
                flushOnStop: true))
            {
                await writer.StartAsync(new CancellationToken(canceled: true));
                await writer.StopAsync();
            }

            using var reopened = new SpoolQueueManager(folder);
            Assert.AreEqual(0, reopened.Count);
        }
        finally
        {
            DeleteFolder(folder);
        }
    }

    /// <summary>
    /// Verifies cancelling a flush leaves every unprocessed record in FIFO order.
    /// </summary>
    [TestMethod]
    public async Task CancelledFlushLeavesPendingRecordsInOrderTestAsync()
    {
        var folder = CreateFolder();

        try
        {
            using (var seed = new SpoolQueueManager(folder))
            {
                seed.Enqueue(CreateAircraft("FIRST"));
                seed.Enqueue(CreateAircraft("SECOND"));
            }

            await using var context = BaseStationReaderDbContextFactory.CreateInMemoryDbContext();
            IDatabaseManagementFactory factory = new DatabaseManagementFactory(new MockFileLogger(), context, 1000);
            await using (IContinuousWriter writer = new ContinuousWriter(
                factory,
                new SpoolQueueManager(folder),
                flushOnStop: false))
            {
                await Assert.ThrowsExactlyAsync<OperationCanceledException>(
                    () => writer.FlushQueueAsync(new CancellationToken(canceled: true)));
            }

            using var reopened = new SpoolQueueManager(folder);
            Assert.AreEqual(2, reopened.Count);
            using var first = reopened.TryDequeue();
            Assert.AreEqual("FIRST", first.Record.TrackedAircraft.Address);
            first.Complete();
            using var second = reopened.TryDequeue();
            Assert.AreEqual("SECOND", second.Record.TrackedAircraft.Address);
            second.Complete();
        }
        finally
        {
            DeleteFolder(folder);
        }
    }

    /// <summary>
    /// Verifies a caller can suppress the configured automatic stop flush.
    /// </summary>
    [TestMethod]
    public async Task StopFlushOverrideLeavesPendingRecordsTestAsync()
    {
        var folder = CreateFolder();

        try
        {
            using (var seed = new SpoolQueueManager(folder))
            {
                seed.Enqueue(CreateAircraft());
            }

            await using var context = BaseStationReaderDbContextFactory.CreateInMemoryDbContext();
            IDatabaseManagementFactory factory = new DatabaseManagementFactory(new MockFileLogger(), context, 1000);
            await using (IContinuousWriter writer = new ContinuousWriter(
                factory,
                new SpoolQueueManager(folder),
                flushOnStop: true))
            {
                await writer.StartAsync(new CancellationToken(canceled: true));
                await writer.StopAsync(flushOnStop: false);
            }

            using var reopened = new SpoolQueueManager(folder);
            Assert.AreEqual(1, reopened.Count);
        }
        finally
        {
            DeleteFolder(folder);
        }
    }

    /// <summary>
    /// Creates a record whose missing session causes a completed failed persistence attempt.
    /// </summary>
    /// <returns>Aircraft record.</returns>
    private static TrackedAircraft CreateAircraft(string address = "FAILED")
        => new()
        {
            Address = address,
            FirstSeen = DateTime.UtcNow,
            LastSeen = DateTime.UtcNow,
            Status = TrackingStatus.Active
        };

    /// <summary>
    /// Creates a session-scoped aircraft record for successful replay.
    /// </summary>
    /// <param name="address">ICAO aircraft address.</param>
    /// <param name="sessionId">Observation session identifier.</param>
    /// <param name="observedAt">Observation timestamp.</param>
    /// <returns>Aircraft record.</returns>
    private static TrackedAircraft CreateAircraft(string address, int sessionId, DateTime observedAt)
        => new()
        {
            Address = address,
            SessionId = sessionId,
            FirstSeen = observedAt,
            LastSeen = observedAt,
            Status = TrackingStatus.Active
        };

    /// <summary>
    /// Creates an isolated spool directory name.
    /// </summary>
    /// <returns>Temporary folder path.</returns>
    private static string CreateFolder()
        => Path.Combine(Path.GetTempPath(), $"BaseStationReader-Writer-{Guid.NewGuid():N}");

    /// <summary>
    /// Removes an isolated spool directory.
    /// </summary>
    /// <param name="folder">Folder to remove.</param>
    private static void DeleteFolder(string folder)
    {
        if (Directory.Exists(folder))
        {
            Directory.Delete(folder, recursive: true);
        }
    }
}

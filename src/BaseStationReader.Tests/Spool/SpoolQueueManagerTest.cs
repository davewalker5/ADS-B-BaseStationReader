using BaseStationReader.BusinessLogic.Spool;
using BaseStationReader.Entities.Spool;
using BaseStationReader.Entities.Tracking;

namespace BaseStationReader.Tests.Spool;

[TestClass]
public sealed class SpoolQueueManagerTest
{
    /// <summary>
    /// Verifies records remain available in FIFO order after the queue is reopened.
    /// </summary>
    [TestMethod]
    public void RecordsSurviveRestartInFifoOrderTest()
    {
        var folder = CreateFolder();

        try
        {
            using (var queue = new SpoolQueueManager(folder))
            {
                queue.Enqueue(CreateAircraft("FIRST"));
                queue.Enqueue(CreateAircraft("SECOND"));
            }

            using var reopened = new SpoolQueueManager(folder);
            Assert.AreEqual(2, reopened.Count);

            using (var first = reopened.TryDequeue())
            {
                Assert.IsNotNull(first);
                Assert.AreEqual(SpoolEntityType.TrackedAircraft, first.Record.EntityType);
                Assert.AreEqual("FIRST", first.Record.TrackedAircraft?.Address);
                first.Complete();
            }

            using (var second = reopened.TryDequeue())
            {
                Assert.IsNotNull(second);
                Assert.AreEqual("SECOND", second.Record.TrackedAircraft?.Address);
                second.Complete();
            }

            Assert.AreEqual(0, reopened.Count);
        }
        finally
        {
            DeleteFolder(folder);
        }
    }

    /// <summary>
    /// Verifies a batch is committed durably and retains its input order.
    /// </summary>
    [TestMethod]
    public void BatchRecordsSurviveRestartInFifoOrderTest()
    {
        var folder = CreateFolder();

        try
        {
            using (var queue = new SpoolQueueManager(folder))
            {
                queue.EnqueueRange([CreateAircraft("FIRST"), CreateAircraft("SECOND")]);
            }

            using var reopened = new SpoolQueueManager(folder);
            Assert.AreEqual(2, reopened.Count);
            using var first = reopened.TryDequeue();
            Assert.AreEqual("FIRST", first?.Record.TrackedAircraft?.Address);
            first?.Complete();
            using var second = reopened.TryDequeue();
            Assert.AreEqual("SECOND", second?.Record.TrackedAircraft?.Address);
            second?.Complete();
        }
        finally
        {
            DeleteFolder(folder);
        }
    }

    /// <summary>
    /// Verifies disposing an incomplete lease restores the record to the head of the queue.
    /// </summary>
    [TestMethod]
    public void IncompleteLeaseIsRolledBackTest()
    {
        var folder = CreateFolder();

        try
        {
            using var queue = new SpoolQueueManager(folder);
            queue.Enqueue(CreateAircraft("ROLLBACK"));

            using (var leased = queue.TryDequeue())
            {
                Assert.IsNotNull(leased);
                Assert.AreEqual("ROLLBACK", leased.Record.TrackedAircraft?.Address);
            }

            using var retried = queue.TryDequeue();
            Assert.IsNotNull(retried);
            Assert.AreEqual("ROLLBACK", retried.Record.TrackedAircraft?.Address);
            retried.Complete();
        }
        finally
        {
            DeleteFolder(folder);
        }
    }

    /// <summary>
    /// Verifies relative spool settings are resolved beside the configured database.
    /// </summary>
    [TestMethod]
    public void RelativeSpoolFolderIsResolvedFromDatabaseFolderTest()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), "tracker-data", "aircraft.db");
        var expected = Path.Combine(Path.GetDirectoryName(databasePath)!, "pending-writes");

        var actual = SpoolFolderResolver.Resolve(
            $"Data Source={databasePath}",
            "pending-writes");

        Assert.AreEqual(Path.GetFullPath(expected), actual);
    }

    /// <summary>
    /// Creates a valid aircraft record for spool tests.
    /// </summary>
    /// <param name="address">Aircraft address.</param>
    /// <returns>Aircraft record.</returns>
    private static TrackedAircraft CreateAircraft(string address)
        => new()
        {
            Address = address,
            SessionId = 12,
            FirstSeen = DateTime.UtcNow,
            LastSeen = DateTime.UtcNow,
            Status = TrackingStatus.Active
        };

    /// <summary>
    /// Creates an isolated queue folder name.
    /// </summary>
    /// <returns>Temporary folder path.</returns>
    private static string CreateFolder()
        => Path.Combine(Path.GetTempPath(), $"BaseStationReader-Spool-{Guid.NewGuid():N}");

    /// <summary>
    /// Removes an isolated queue directory after its owner has been disposed.
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

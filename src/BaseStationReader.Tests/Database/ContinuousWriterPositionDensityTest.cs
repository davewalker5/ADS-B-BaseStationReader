using BaseStationReader.BusinessLogic.Database;
using BaseStationReader.Data;
using BaseStationReader.Entities.History;
using BaseStationReader.Entities.Tracking;
using BaseStationReader.Interfaces.Database;
using BaseStationReader.Tests.Mocks;

namespace BaseStationReader.Tests.Database;

[TestClass]
public sealed class ContinuousWriterPositionDensityTest
{
    /// <summary>
    /// Verifies shutdown drains a queued complete snapshot through the persistence manager.
    /// </summary>
    [TestMethod]
    public async Task StopFlushesQueuedSnapshotTestAsync()
    {
        await using var context = BaseStationReaderDbContextFactory.CreateInMemoryDbContext();
        var session = new ObservationSession
        {
            Name = "Density writer test",
            StartedAtUtc = DateTime.UtcNow,
            ProfileName = "Density writer test",
            Host = "receiver.local",
            Port = 30003,
            IncludedBehaviours = "Unknown"
        };
        context.ObservationSessions.Add(session);
        await context.SaveChangesAsync();
        IDatabaseManagementFactory factory = new DatabaseManagementFactory(new MockFileLogger(), context, 1000);
        await using IContinuousWriter writer = new ContinuousWriter(factory);
        await writer.StartAsync(CancellationToken.None);

        writer.Push(CreateSnapshot(session.Id));
        await writer.StopAsync();

        var saved = context.PositionDensitySnapshots.Single();
        Assert.AreEqual(session.Id, saved.SessionId);
        Assert.HasCount(1, context.PositionDensitySnapshotCells);
    }

    /// <summary>
    /// Creates a valid complete persistence request for the continuous writer.
    /// </summary>
    /// <param name="sessionId"></param>
    /// <returns></returns>
    private static PositionDensitySnapshotEntity CreateSnapshot(int sessionId)
    {
        // The request mirrors the mapper output consumed by the production queue.
        return new PositionDensitySnapshotEntity
        {
            SessionId = sessionId,
            CapturedAtUtc = DateTime.UtcNow,
            PositionCount = 2,
            MaximumBinCount = 2,
            MinimumLatitude = 50d,
            MaximumLatitude = 52d,
            MinimumLongitude = -1d,
            MaximumLongitude = 1d,
            Cells =
            [
                new PositionDensitySnapshotCellEntity { Latitude = 51d, Longitude = 0d, Count = 2 }
            ]
        };
    }
}

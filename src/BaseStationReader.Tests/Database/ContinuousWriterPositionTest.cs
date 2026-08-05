using BaseStationReader.BusinessLogic.Database;
using BaseStationReader.Data;
using BaseStationReader.Entities.Tracking;
using BaseStationReader.Interfaces.Database;
using BaseStationReader.Tests.Mocks;

namespace BaseStationReader.Tests.Database;

[TestClass]
public sealed class ContinuousWriterPositionTest
{
    /// <summary>
    /// Verifies a queued position is attached to the aircraft from its originating session when
    /// another session contains the same aircraft address.
    /// </summary>
    [TestMethod]
    public async Task PositionIsAssociatedWithAircraftFromMatchingSessionTestAsync()
    {
        await using var context = BaseStationReaderDbContextFactory.CreateInMemoryDbContext();
        var firstSession = CreateSession("First session");
        var secondSession = CreateSession("Second session");
        context.ObservationSessions.AddRange(firstSession, secondSession);
        await context.SaveChangesAsync();

        var now = DateTime.Now;
        var firstAircraft = CreateAircraft("406A3D", firstSession.Id, now.AddMilliseconds(-1));
        var secondAircraft = CreateAircraft("406A3D", secondSession.Id, now);
        context.TrackedAircraft.AddRange(firstAircraft, secondAircraft);
        await context.SaveChangesAsync();

        IDatabaseManagementFactory factory = new DatabaseManagementFactory(new MockFileLogger(), context, 1000);
        await using IContinuousWriter writer = new ContinuousWriter(factory);
        await writer.StartAsync(CancellationToken.None);

        writer.Push(new AircraftPosition
        {
            Address = firstAircraft.Address,
            SessionId = firstSession.Id,
            Timestamp = now
        });
        await writer.StopAsync();

        var saved = context.Positions.Single();
        Assert.AreEqual(firstAircraft.Id, saved.AircraftId);
        Assert.AreNotEqual(secondAircraft.Id, saved.AircraftId);
    }

    /// <summary>
    /// Verifies position creation copies the session identifier needed while queued.
    /// </summary>
    [TestMethod]
    public void FromTrackedAircraftCopiesSessionIdTest()
    {
        var aircraft = CreateAircraft("406A3D", 42, DateTime.Now);

        var position = AircraftPosition.FromTrackedAircraft(aircraft);

        Assert.AreEqual(aircraft.SessionId, position.SessionId);
    }

    private static ObservationSession CreateSession(string name)
        => new()
        {
            Name = name,
            StartedAtUtc = DateTime.UtcNow,
            ProfileName = "Writer test",
            Host = "receiver.local",
            Port = 30003,
            IncludedBehaviours = "Unknown"
        };

    private static TrackedAircraft CreateAircraft(string address, int sessionId, DateTime lastSeen)
        => new()
        {
            Address = address,
            SessionId = sessionId,
            FirstSeen = lastSeen,
            LastSeen = lastSeen,
            Status = TrackingStatus.Active
        };
}

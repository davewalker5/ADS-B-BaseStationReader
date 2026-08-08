using BaseStationReader.BusinessLogic.Database;
using BaseStationReader.Data;
using BaseStationReader.Entities.Tracking;
using BaseStationReader.Interfaces.Database;
using BaseStationReader.Tests.Mocks;

namespace BaseStationReader.Tests.Database;

[TestClass]
public sealed class ContinuousWriterAircraftLifetimeTest
{
    private const string Address = "40092B";
    private const int TimeToLockMs = 900000;

    /// <summary>
    /// Verifies a historical deferred sequence produces one lifetime when its observation gaps are short.
    /// </summary>
    [TestMethod]
    public async Task DeferredHistoricalSequenceProducesOneLifetimeTestAsync()
    {
        await using var context = BaseStationReaderDbContextFactory.CreateInMemoryDbContext();
        var session = await AddSessionAsync(context);
        var observedAt = DateTime.UtcNow.AddHours(-2);
        await using var writer = CreateWriter(context, flushWhileActive: false);
        await writer.StartAsync(CancellationToken.None);

        for (var index = 0; index < 120; index++)
        {
            writer.Push(CreateAircraft(session.Id, observedAt.AddSeconds(index), index + 1));
        }

        await writer.StopAsync();

        var records = context.TrackedAircraft.Where(aircraft => aircraft.SessionId == session.Id).ToArray();
        Assert.HasCount(1, records);
        Assert.AreEqual(observedAt, records[0].FirstSeen);
        Assert.AreEqual(observedAt.AddSeconds(119), records[0].LastSeen);
        Assert.AreEqual(120, records[0].Messages);
    }

    /// <summary>
    /// Verifies active draining and deferred flushing produce identical lifetime boundaries.
    /// </summary>
    [TestMethod]
    public async Task ImmediateAndDeferredProcessingProduceEquivalentLifetimesTestAsync()
    {
        var firstObservedAt = DateTime.UtcNow.AddDays(-1);
        var immediate = await ProcessSequenceAsync(firstObservedAt, flushWhileActive: true);
        var deferred = await ProcessSequenceAsync(firstObservedAt, flushWhileActive: false);

        Assert.HasCount(immediate.Length, deferred);
        for (var index = 0; index < immediate.Length; index++)
        {
            Assert.AreEqual(immediate[index].FirstSeen, deferred[index].FirstSeen);
            Assert.AreEqual(immediate[index].LastSeen, deferred[index].LastSeen);
            Assert.AreEqual(immediate[index].Messages, deferred[index].Messages);
            Assert.AreEqual(immediate[index].Status, deferred[index].Status);
        }
    }

    /// <summary>
    /// Verifies a stale lifetime is reused and restored to active when an aircraft returns within the boundary.
    /// </summary>
    [TestMethod]
    public async Task ReturningStaleAircraftReusesLifetimeAndPreservesAggregatesTestAsync()
    {
        await using var context = BaseStationReaderDbContextFactory.CreateInMemoryDbContext();
        var session = await AddSessionAsync(context);
        var firstSeen = DateTime.UtcNow.AddHours(-2);
        await using var writer = CreateWriter(context, flushWhileActive: false);
        await writer.StartAsync(CancellationToken.None);

        writer.Push(CreateAircraft(session.Id, firstSeen, 75, TrackingStatus.Stale));
        var returned = CreateAircraft(session.Id, firstSeen.AddMinutes(5), 1, TrackingStatus.Active);
        returned.FirstSeen = returned.LastSeen;
        returned.Callsign = "LOG942C";
        writer.Push(returned);
        await writer.StopAsync();

        var record = context.TrackedAircraft.Single(aircraft => aircraft.SessionId == session.Id);
        Assert.AreEqual(firstSeen, record.FirstSeen);
        Assert.AreEqual(returned.LastSeen, record.LastSeen);
        Assert.AreEqual(75, record.Messages);
        Assert.AreEqual(TrackingStatus.Active, record.Status);
        Assert.AreEqual(returned.Callsign, record.Callsign);
    }

    /// <summary>
    /// Verifies a genuine lock-boundary gap creates a new lifetime and positions follow that boundary.
    /// </summary>
    [TestMethod]
    public async Task GenuineGapCreatesNewLifetimeAndPositionsFollowItTestAsync()
    {
        await using var context = BaseStationReaderDbContextFactory.CreateInMemoryDbContext();
        var session = await AddSessionAsync(context);
        var firstObservedAt = DateTime.UtcNow.AddHours(-2);
        var secondObservedAt = firstObservedAt.AddMilliseconds(TimeToLockMs);
        await using var writer = CreateWriter(context, flushWhileActive: false);
        await writer.StartAsync(CancellationToken.None);

        writer.Push(CreateAircraft(session.Id, firstObservedAt, 1));
        writer.Push(CreatePosition(session.Id, firstObservedAt));
        writer.Push(CreateAircraft(session.Id, secondObservedAt, 1));
        writer.Push(CreatePosition(session.Id, secondObservedAt));
        await writer.StopAsync();

        var records = context.TrackedAircraft.OrderBy(aircraft => aircraft.LastSeen).ToArray();
        var positions = context.Positions.OrderBy(position => position.Timestamp).ToArray();
        Assert.HasCount(2, records);
        Assert.HasCount(2, positions);
        Assert.AreEqual(TrackingStatus.Locked, records[0].Status);
        Assert.AreEqual(TrackingStatus.Active, records[1].Status);
        Assert.AreEqual(records[0].Id, positions[0].AircraftId);
        Assert.AreEqual(records[1].Id, positions[1].AircraftId);
    }

    /// <summary>
    /// Verifies an out-of-order snapshot cannot regress lifetime fields or latest aircraft properties.
    /// </summary>
    [TestMethod]
    public async Task OutOfOrderSnapshotDoesNotRegressLifetimeTestAsync()
    {
        await using var context = BaseStationReaderDbContextFactory.CreateInMemoryDbContext();
        var session = await AddSessionAsync(context);
        var firstSeen = DateTime.UtcNow.AddHours(-2);
        var latest = CreateAircraft(session.Id, firstSeen.AddMinutes(2), 20);
        latest.FirstSeen = firstSeen;
        latest.Altitude = 20000;
        var older = CreateAircraft(session.Id, firstSeen.AddMinutes(1), 10, TrackingStatus.Inactive);
        older.FirstSeen = firstSeen;
        older.Altitude = 10000;
        await using var writer = CreateWriter(context, flushWhileActive: false);
        await writer.StartAsync(CancellationToken.None);

        writer.Push(latest);
        writer.Push(older);
        await writer.StopAsync();

        var record = context.TrackedAircraft.Single();
        Assert.AreEqual(latest.LastSeen, record.LastSeen);
        Assert.AreEqual(latest.Messages, record.Messages);
        Assert.AreEqual(latest.Altitude, record.Altitude);
        Assert.AreEqual(latest.Status, record.Status);
    }

    /// <summary>
    /// Creates a continuous writer with the required persistence services.
    /// </summary>
    /// <param name="context">Persistence context.</param>
    /// <param name="flushWhileActive">Whether the queue is drained before stop.</param>
    /// <returns>Continuous writer.</returns>
    private static IContinuousWriter CreateWriter(
        BaseStationReaderDbContext context,
        bool flushWhileActive)
    {
        IDatabaseManagementFactory factory = new DatabaseManagementFactory(
            new MockFileLogger(),
            context,
            TimeToLockMs);
        return new ContinuousWriter(
            factory,
            new TemporarySpoolQueue(),
            flushOnStop: true,
            flushWhileActive: flushWhileActive);
    }

    /// <summary>
    /// Processes the same historical sequence using one continuous-writer mode.
    /// </summary>
    /// <param name="firstObservedAt">Timestamp of the first observation.</param>
    /// <param name="flushWhileActive">Whether queued records are processed before stop.</param>
    /// <returns>Ordered snapshots of the resulting lifetimes.</returns>
    private static async Task<TrackedAircraft[]> ProcessSequenceAsync(
        DateTime firstObservedAt,
        bool flushWhileActive)
    {
        await using var context = BaseStationReaderDbContextFactory.CreateInMemoryDbContext();
        var session = await AddSessionAsync(context);
        await using var writer = CreateWriter(context, flushWhileActive);
        await writer.StartAsync(CancellationToken.None);

        writer.Push(CreateAircraft(session.Id, firstObservedAt, 1));
        writer.Push(CreateAircraft(session.Id, firstObservedAt.AddMinutes(1), 2));
        writer.Push(CreateAircraft(session.Id, firstObservedAt.AddMinutes(16), 1));
        await writer.StopAsync();

        return context.TrackedAircraft
            .OrderBy(aircraft => aircraft.FirstSeen)
            .Select(aircraft => (TrackedAircraft)aircraft.Clone())
            .ToArray();
    }

    /// <summary>
    /// Adds an observation session to the supplied context.
    /// </summary>
    /// <param name="context">Persistence context.</param>
    /// <returns>Persisted observation session.</returns>
    private static async Task<ObservationSession> AddSessionAsync(BaseStationReaderDbContext context)
    {
        var session = new ObservationSession
        {
            Name = "Deferred writer test",
            StartedAtUtc = DateTime.UtcNow,
            ProfileName = "Test",
            Host = "receiver.local",
            Port = 30003,
            IncludedBehaviours = "Unknown"
        };
        context.ObservationSessions.Add(session);
        await context.SaveChangesAsync();
        return session;
    }

    /// <summary>
    /// Creates a queued aircraft snapshot.
    /// </summary>
    /// <param name="sessionId">Observation session identifier.</param>
    /// <param name="observedAt">Observation timestamp.</param>
    /// <param name="messages">In-memory message count.</param>
    /// <param name="status">Tracking status.</param>
    /// <returns>Queued aircraft snapshot.</returns>
    private static TrackedAircraft CreateAircraft(
        int sessionId,
        DateTime observedAt,
        int messages,
        TrackingStatus status = TrackingStatus.Active)
        => new()
        {
            Address = Address,
            SessionId = sessionId,
            FirstSeen = observedAt,
            LastSeen = observedAt,
            Messages = messages,
            Status = status
        };

    /// <summary>
    /// Creates a queued aircraft position.
    /// </summary>
    /// <param name="sessionId">Observation session identifier.</param>
    /// <param name="observedAt">Observation timestamp.</param>
    /// <returns>Queued position.</returns>
    private static AircraftPosition CreatePosition(int sessionId, DateTime observedAt)
        => new()
        {
            Address = Address,
            SessionId = sessionId,
            Timestamp = observedAt
        };
}

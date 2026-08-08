using BaseStationReader.BusinessLogic.Database;
using BaseStationReader.Data;
using BaseStationReader.Entities.Tracking;
using BaseStationReader.Interfaces.Database;
using BaseStationReader.Tests.Mocks;

namespace BaseStationReader.Tests.Database;

[TestClass]
public sealed class AircraftLifetimeManagerTest
{
    private const int TimeToLockMs = 600000;
    private const string Address = "406A3D";

    private BaseStationReaderDbContext _context = null!;
    private IDatabaseManagementFactory _factory = null!;
    private ObservationSession _session = null!;

    /// <summary>
    /// Creates an isolated persistence context and observation session.
    /// </summary>
    [TestInitialize]
    public async Task TestInitialiseAsync()
    {
        _context = BaseStationReaderDbContextFactory.CreateInMemoryDbContext();
        _factory = new DatabaseManagementFactory(new MockFileLogger(), _context, TimeToLockMs);
        _session = CreateSession();
        _context.ObservationSessions.Add(_session);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Releases the isolated persistence context.
    /// </summary>
    [TestCleanup]
    public async Task TestCleanupAsync()
        => await _context.DisposeAsync();

    /// <summary>
    /// Verifies processing age cannot split observations with a short observation gap.
    /// </summary>
    [TestMethod]
    public async Task HistoricalObservationWithinBoundaryReusesLifetimeTestAsync()
    {
        var firstSeen = DateTime.UtcNow.AddDays(-2);
        var existing = await AddAircraftAsync(firstSeen);

        var resolved = await _factory.AircraftLifetimeManager.ResolveAsync(
            Address,
            _session.Id,
            firstSeen.AddMinutes(1));

        Assert.IsNotNull(resolved);
        Assert.AreEqual(existing.Id, resolved.Id);
    }

    /// <summary>
    /// Verifies a gap exactly equal to the configured boundary locks the previous lifetime.
    /// </summary>
    [TestMethod]
    public async Task ObservationAtBoundaryLocksLifetimeTestAsync()
    {
        var lastSeen = DateTime.UtcNow.AddHours(-2);
        var existing = await AddAircraftAsync(lastSeen);

        var resolved = await _factory.AircraftLifetimeManager.ResolveAsync(
            Address,
            _session.Id,
            lastSeen.AddMilliseconds(TimeToLockMs));

        Assert.IsNull(resolved);
        var locked = await _factory.TrackedAircraftWriter.GetAsync(aircraft => aircraft.Id == existing.Id);
        Assert.AreEqual(TrackingStatus.Locked, locked.Status);
    }

    /// <summary>
    /// Verifies equal and out-of-order observations remain in the current lifetime.
    /// </summary>
    [TestMethod]
    public async Task NonPositiveObservationGapReusesLifetimeTestAsync()
    {
        var lastSeen = DateTime.UtcNow.AddHours(-2);
        var existing = await AddAircraftAsync(lastSeen);

        var equal = await _factory.AircraftLifetimeManager.ResolveAsync(Address, _session.Id, lastSeen);
        var older = await _factory.AircraftLifetimeManager.ResolveAsync(
            Address,
            _session.Id,
            lastSeen.AddSeconds(-1));

        Assert.AreEqual(existing.Id, equal.Id);
        Assert.AreEqual(existing.Id, older.Id);
    }

    /// <summary>
    /// Verifies lifetime lookup is restricted to the supplied observation session.
    /// </summary>
    [TestMethod]
    public async Task ResolveDoesNotReuseAnotherSessionTestAsync()
    {
        var observedAt = DateTime.UtcNow.AddHours(-2);
        await AddAircraftAsync(observedAt);
        var otherSession = CreateSession();
        _context.ObservationSessions.Add(otherSession);
        await _context.SaveChangesAsync();

        var resolved = await _factory.AircraftLifetimeManager.ResolveAsync(
            Address,
            otherSession.Id,
            observedAt.AddSeconds(1));

        Assert.IsNull(resolved);
    }

    /// <summary>
    /// Verifies a locked lifetime cannot be resurrected.
    /// </summary>
    [TestMethod]
    public async Task LockedLifetimeIsNotReusedTestAsync()
    {
        var observedAt = DateTime.UtcNow.AddHours(-2);
        var existing = await AddAircraftAsync(observedAt);
        existing.Status = TrackingStatus.Locked;
        await _factory.TrackedAircraftWriter.WriteAsync(existing);

        var resolved = await _factory.AircraftLifetimeManager.ResolveAsync(
            Address,
            _session.Id,
            observedAt.AddSeconds(1));

        Assert.IsNull(resolved);
    }

    /// <summary>
    /// Adds one persisted aircraft lifetime to the current session.
    /// </summary>
    /// <param name="lastSeen">Last observation timestamp.</param>
    /// <returns>Persisted lifetime.</returns>
    private async Task<TrackedAircraft> AddAircraftAsync(DateTime lastSeen)
        => await _factory.TrackedAircraftWriter.WriteAsync(new TrackedAircraft
        {
            Address = Address,
            SessionId = _session.Id,
            FirstSeen = lastSeen,
            LastSeen = lastSeen,
            Status = TrackingStatus.Active
        });

    /// <summary>
    /// Creates an observation session for lifetime isolation.
    /// </summary>
    /// <returns>Unpersisted observation session.</returns>
    private static ObservationSession CreateSession()
        => new()
        {
            Name = "Lifetime test",
            StartedAtUtc = DateTime.UtcNow,
            ProfileName = "Test",
            Host = "receiver.local",
            Port = 30003,
            IncludedBehaviours = "Unknown"
        };
}

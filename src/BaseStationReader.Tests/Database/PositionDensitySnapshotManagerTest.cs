using BaseStationReader.BusinessLogic.Database;
using BaseStationReader.Data;
using BaseStationReader.Entities.History;
using BaseStationReader.Entities.Tracking;
using BaseStationReader.Interfaces.Database;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace BaseStationReader.Tests.Database;

[TestClass]
public sealed class PositionDensitySnapshotManagerTest
{
    private SqliteConnection _connection = null!;
    private BaseStationReaderDbContext _context = null!;
    private IPositionDensitySnapshotManager _manager = null!;
    private int _sessionId;

    /// <summary>
    /// Creates a relational in-memory database for each persistence test.
    /// </summary>
    [TestInitialize]
    public async Task InitialiseAsync()
    {
        // SQLite exercises foreign keys, indexes, constraints, and transactions together.
        _connection = new SqliteConnection("Data Source=:memory:");
        await _connection.OpenAsync();
        var options = new DbContextOptionsBuilder<BaseStationReaderDbContext>()
            .UseSqlite(_connection)
            .Options;
        _context = new BaseStationReaderDbContext(options);
        await _context.Database.EnsureCreatedAsync();
        var session = new ObservationSession
        {
            Name = "Snapshot test",
            StartedAtUtc = DateTime.UtcNow,
            ProfileName = "Snapshot test",
            Host = "receiver.local",
            Port = 30003,
            IncludedBehaviours = "Unknown"
        };
        _context.ObservationSessions.Add(session);
        await _context.SaveChangesAsync();
        _sessionId = session.Id;
        _manager = new PositionDensitySnapshotManager(_context);
    }

    /// <summary>
    /// Releases the relational test database.
    /// </summary>
    [TestCleanup]
    public async Task CleanupAsync()
    {
        // Dispose both owners of the in-memory connection after each test.
        await _context.DisposeAsync();
        await _connection.DisposeAsync();
    }

    /// <summary>
    /// Verifies a complete snapshot is written and reconstructed in stable cell order.
    /// </summary>
    [TestMethod]
    public async Task AddAndGetCompleteSnapshotTestAsync()
    {
        var snapshot = CreateSnapshot();

        var id = await _manager.AddAsync(snapshot);
        _context.ChangeTracker.Clear();
        var loaded = await _manager.GetByIdAsync(id);

        Assert.IsNotNull(loaded);
        Assert.AreEqual(_sessionId, loaded.SessionId);
        Assert.AreEqual(snapshot.CapturedAtUtc, loaded.CapturedAtUtc);
        Assert.AreEqual(3, loaded.PositionCount);
        Assert.HasCount(2, loaded.Cells);
        Assert.AreEqual(51.1d, loaded.Cells.First().Latitude);
        Assert.IsEmpty(_context.ChangeTracker.Entries());
    }

    /// <summary>
    /// Verifies session listings contain ordered metadata without loading cells.
    /// </summary>
    [TestMethod]
    public async Task GetForSessionReturnsOrderedMetadataTestAsync()
    {
        var later = CreateSnapshot(DateTime.UtcNow.AddMinutes(1));
        var earlier = CreateSnapshot(DateTime.UtcNow);
        await _manager.AddAsync(later);
        await _manager.AddAsync(earlier);
        _context.ChangeTracker.Clear();

        var snapshots = await _manager.GetForSessionAsync(_sessionId);

        Assert.HasCount(2, snapshots);
        Assert.IsTrue(snapshots[0].CapturedAtUtc < snapshots[1].CapturedAtUtc);
        Assert.IsTrue(snapshots.All(item => item.Cells.Count == 0));
    }

    /// <summary>
    /// Verifies latest retrieval uses the identifier to break equal capture-time ties.
    /// </summary>
    [TestMethod]
    public async Task GetLatestForSessionUsesIdentifierTieBreakTestAsync()
    {
        var capturedAtUtc = DateTime.UtcNow;
        await _manager.AddAsync(CreateSnapshot(capturedAtUtc));
        var expectedId = await _manager.AddAsync(CreateSnapshot(capturedAtUtc));
        _context.ChangeTracker.Clear();

        var latest = await _manager.GetLatestForSessionAsync(_sessionId);

        Assert.IsNotNull(latest);
        Assert.AreEqual(expectedId, latest.Id);
        Assert.HasCount(2, latest.Cells);
    }

    /// <summary>
    /// Verifies invalid cells are rejected before any snapshot records are written.
    /// </summary>
    [TestMethod]
    public async Task AddRejectsDuplicateCellsAtomicallyTestAsync()
    {
        var snapshot = CreateSnapshot();
        snapshot.Cells.Add(new PositionDensitySnapshotCellEntity
        {
            Latitude = 51.1d,
            Longitude = -0.2d,
            Count = 1
        });

        await Assert.ThrowsExactlyAsync<ArgumentException>(() => _manager.AddAsync(snapshot));

        Assert.IsEmpty(await _context.PositionDensitySnapshots.ToListAsync());
        Assert.IsEmpty(await _context.PositionDensitySnapshotCells.ToListAsync());
    }

    /// <summary>
    /// Verifies deleting a session cascades through snapshots to their cells.
    /// </summary>
    [TestMethod]
    public async Task SessionDeletionCascadesToSnapshotsAndCellsTestAsync()
    {
        await _manager.AddAsync(CreateSnapshot());
        _context.ChangeTracker.Clear();

        var session = await _context.ObservationSessions.SingleAsync(item => item.Id == _sessionId);
        _context.ObservationSessions.Remove(session);
        await _context.SaveChangesAsync();

        Assert.IsEmpty(await _context.PositionDensitySnapshots.ToListAsync());
        Assert.IsEmpty(await _context.PositionDensitySnapshotCells.ToListAsync());
    }

    /// <summary>
    /// Creates a valid snapshot matching the geographic-bin density model.
    /// </summary>
    /// <param name="capturedAtUtc"></param>
    /// <returns></returns>
    private PositionDensitySnapshotEntity CreateSnapshot(DateTime? capturedAtUtc = null)
    {
        // The cells are deliberately supplied out of order to exercise deterministic reads.
        return new PositionDensitySnapshotEntity
        {
            SessionId = _sessionId,
            CapturedAtUtc = capturedAtUtc ?? DateTime.UtcNow,
            PositionCount = 3,
            MaximumBinCount = 2,
            MinimumLatitude = 50d,
            MaximumLatitude = 52d,
            MinimumLongitude = -1d,
            MaximumLongitude = 1d,
            Cells =
            [
                new PositionDensitySnapshotCellEntity { Latitude = 51.5d, Longitude = 0.2d, Count = 2 },
                new PositionDensitySnapshotCellEntity { Latitude = 51.1d, Longitude = -0.2d, Count = 1 }
            ]
        };
    }
}

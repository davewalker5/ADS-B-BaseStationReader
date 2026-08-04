using BaseStationReader.Data;
using BaseStationReader.Entities.History;
using BaseStationReader.Interfaces.Database;
using Microsoft.EntityFrameworkCore;

#nullable enable

namespace BaseStationReader.BusinessLogic.Database;

internal sealed class PositionDensitySnapshotManager : IPositionDensitySnapshotManager
{
    private readonly BaseStationReaderDbContext _context;

    /// <summary>
    /// Creates a position-density snapshot manager.
    /// </summary>
    /// <param name="context"></param>
    public PositionDensitySnapshotManager(BaseStationReaderDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<int> AddAsync(PositionDensitySnapshotEntity snapshot, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        Validate(snapshot);

        // Check ownership explicitly so providers without relational foreign keys behave consistently.
        if (!await _context.ObservationSessions.AnyAsync(item => item.Id == snapshot.SessionId, cancellationToken))
        {
            throw new ArgumentException("The snapshot session does not exist.", nameof(snapshot));
        }

        // A transaction makes the header and its complete cell collection one persistence unit.
        await using var transaction = _context.Database.IsRelational()
            ? await _context.Database.BeginTransactionAsync(cancellationToken)
            : null;
        try
        {
            await _context.PositionDensitySnapshots.AddAsync(snapshot, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            if (transaction is not null)
            {
                await transaction.CommitAsync(cancellationToken);
            }
            return snapshot.Id;
        }
        catch
        {
            if (transaction is not null)
            {
                await transaction.RollbackAsync(cancellationToken);
            }
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<PositionDensitySnapshotEntity?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id));
        }

        // Materialise cells in geographic order so every read has deterministic output.
        var snapshot = await _context.PositionDensitySnapshots.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (snapshot is null)
        {
            return null;
        }

        snapshot.Cells = await _context.PositionDensitySnapshotCells.AsNoTracking()
            .Where(item => item.PositionDensitySnapshotId == id)
            .OrderBy(item => item.Latitude)
            .ThenBy(item => item.Longitude)
            .ToListAsync(cancellationToken);
        return snapshot;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PositionDensitySnapshotEntity>> GetForSessionAsync(int sessionId, CancellationToken cancellationToken = default)
    {
        if (sessionId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sessionId));
        }

        // Listing snapshots intentionally excludes cells; callers can request one complete snapshot on demand.
        return await _context.PositionDensitySnapshots.AsNoTracking()
            .Where(item => item.SessionId == sessionId)
            .OrderBy(item => item.CapturedAtUtc)
            .ThenBy(item => item.Id)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<PositionDensitySnapshotEntity?> GetLatestForSessionAsync(
        int sessionId,
        CancellationToken cancellationToken = default)
    {
        if (sessionId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sessionId));
        }

        // Resolve the identifier first so the detail path remains the single source for complete reconstruction.
        var id = await _context.PositionDensitySnapshots.AsNoTracking()
            .Where(item => item.SessionId == sessionId)
            .OrderByDescending(item => item.CapturedAtUtc)
            .ThenByDescending(item => item.Id)
            .Select(item => (int?)item.Id)
            .FirstOrDefaultAsync(cancellationToken);
        return id.HasValue ? await GetByIdAsync(id.Value, cancellationToken) : null;
    }

    /// <summary>
    /// Validates snapshot metadata and every populated cell before opening a transaction.
    /// </summary>
    /// <param name="snapshot"></param>
    /// <exception cref="ArgumentException"></exception>
    private static void Validate(PositionDensitySnapshotEntity snapshot)
    {
        if (snapshot.Id != 0)
        {
            throw new ArgumentException("A new snapshot cannot already have an identifier.", nameof(snapshot));
        }
        if (snapshot.SessionId <= 0)
        {
            throw new ArgumentException("A snapshot must have a valid session identifier.", nameof(snapshot));
        }
        if (snapshot.CapturedAtUtc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("The capture time must be UTC.", nameof(snapshot));
        }
        if (snapshot.PositionCount < 0 || snapshot.MaximumBinCount < 0)
        {
            throw new ArgumentException("Snapshot counts cannot be negative.", nameof(snapshot));
        }
        if (snapshot.Cells is null)
        {
            throw new ArgumentException("A snapshot must have a cell collection.", nameof(snapshot));
        }
        if (!ValidLatitude(snapshot.MinimumLatitude) || !ValidLatitude(snapshot.MaximumLatitude) || snapshot.MaximumLatitude < snapshot.MinimumLatitude)
        {
            throw new ArgumentException("Snapshot latitude bounds are invalid.", nameof(snapshot));
        }
        if (!ValidLongitude(snapshot.MinimumLongitude) || !ValidLongitude(snapshot.MaximumLongitude) || snapshot.MaximumLongitude < snapshot.MinimumLongitude)
        {
            throw new ArgumentException("Snapshot longitude bounds are invalid.", nameof(snapshot));
        }

        var coordinates = new HashSet<(double Latitude, double Longitude)>();
        foreach (var cell in snapshot.Cells)
        {
            // Cells use the stable geographic identities exposed by the existing density model.
            if (cell.Id != 0 || cell.PositionDensitySnapshotId != 0)
            {
                throw new ArgumentException("New cells cannot already have identifiers.", nameof(snapshot));
            }
            if (!ValidLatitude(cell.Latitude) || !ValidLongitude(cell.Longitude) || cell.Count <= 0)
            {
                throw new ArgumentException("A snapshot cell is invalid.", nameof(snapshot));
            }
            if (cell.Latitude < snapshot.MinimumLatitude || cell.Latitude > snapshot.MaximumLatitude ||
                cell.Longitude < snapshot.MinimumLongitude || cell.Longitude > snapshot.MaximumLongitude)
            {
                throw new ArgumentException("A snapshot cell lies outside the snapshot bounds.", nameof(snapshot));
            }
            if (!coordinates.Add((cell.Latitude, cell.Longitude)))
            {
                throw new ArgumentException("A snapshot contains duplicate cell coordinates.", nameof(snapshot));
            }
        }

    }

    /// <summary>
    /// Determines whether a value is a finite latitude.
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    private static bool ValidLatitude(double value) => double.IsFinite(value) && value is >= -90d and <= 90d;

    /// <summary>
    /// Determines whether a value is a finite longitude.
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    private static bool ValidLongitude(double value) => double.IsFinite(value) && value is >= -180d and <= 180d;
}

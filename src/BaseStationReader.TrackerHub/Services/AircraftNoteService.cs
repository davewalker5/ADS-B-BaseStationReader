using BaseStationReader.BusinessLogic.Database;
using BaseStationReader.Data;
using BaseStationReader.Entities.Tracking;
using BaseStationReader.Interfaces.Logging;
using Microsoft.EntityFrameworkCore;

namespace BaseStationReader.TrackerHub.Services;

/// <summary>Creates short-lived database managers for aircraft-note UI operations.</summary>
public sealed class AircraftNoteService : IAircraftNoteService
{
    private readonly IDbContextFactory<BaseStationReaderDbContext> _contextFactory;
    private readonly ITrackerLogger _logger;

    /// <summary>Initialises an aircraft-note service.</summary>
    public AircraftNoteService(IDbContextFactory<BaseStationReaderDbContext> contextFactory, ITrackerLogger logger)
    {
        _contextFactory = contextFactory;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<AircraftNote> AddAsync(string address, string noteText, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        return await new DatabaseManagementFactory(_logger, context, 0).AircraftNoteManager
            .AddAsync(address, noteText, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AircraftNote>> ListAsync(string address, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        return await new DatabaseManagementFactory(_logger, context, 0).AircraftNoteManager
            .ListAsync(address, cancellationToken);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(int id, string address, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        await new DatabaseManagementFactory(_logger, context, 0).AircraftNoteManager
            .DeleteAsync(id, address, cancellationToken);
    }
}

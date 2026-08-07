#nullable enable

using BaseStationReader.BusinessLogic.Database;
using BaseStationReader.Data;
using BaseStationReader.Entities.Api;
using BaseStationReader.Interfaces.Logging;
using Microsoft.EntityFrameworkCore;

namespace BaseStationReader.TrackerHub.Services;

/// <summary>
/// Creates short-lived database managers for excluded-callsign UI operations.
/// </summary>
public sealed class ExcludedCallsignService : IExcludedCallsignService
{
    private readonly IDbContextFactory<BaseStationReaderDbContext> _contextFactory;
    private readonly ITrackerLogger _logger;

    /// <summary>
    /// Initialises a new excluded-callsign service.
    /// </summary>
    /// <param name="contextFactory">The database context factory.</param>
    /// <param name="logger">The application logger.</param>
    public ExcludedCallsignService(
        IDbContextFactory<BaseStationReaderDbContext> contextFactory,
        ITrackerLogger logger)
    {
        _contextFactory = contextFactory;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ExcludedCallsign>> SearchAsync(
        string? callsign,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var manager = new DatabaseManagementFactory(_logger, context, 0).ExcludedCallsignManager;
        return await manager.SearchAsync(callsign, cancellationToken);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(string callsign, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var manager = new DatabaseManagementFactory(_logger, context, 0).ExcludedCallsignManager;
        await manager.DeleteAsync(callsign, cancellationToken);
    }
}

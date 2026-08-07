using BaseStationReader.BusinessLogic.Database;
using BaseStationReader.Data;
using BaseStationReader.Entities.Api;
using BaseStationReader.Entities.History;
using BaseStationReader.Interfaces.Logging;
using Microsoft.EntityFrameworkCore;

namespace BaseStationReader.TrackerHub.Services;

/// <summary>
/// Creates short-lived database managers for API log UI operations.
/// </summary>
public sealed class ApiLogService : IApiLogService
{
    private readonly IDbContextFactory<BaseStationReaderDbContext> _contextFactory;
    private readonly ITrackerLogger _logger;

    /// <summary>
    /// Initialises a new API log service.
    /// </summary>
    /// <param name="contextFactory">The database context factory.</param>
    /// <param name="logger">The application logger.</param>
    public ApiLogService(
        IDbContextFactory<BaseStationReaderDbContext> contextFactory,
        ITrackerLogger logger)
    {
        _contextFactory = contextFactory;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<PagedResult<ApiLogEntry>> SearchAsync(
        ApiLogFilter filter,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var manager = new DatabaseManagementFactory(_logger, context, 0).ApiLogManager;
        return await manager.SearchAsync(filter, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> ClearAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var manager = new DatabaseManagementFactory(_logger, context, 0).ApiLogManager;
        return await manager.ClearAsync(cancellationToken);
    }
}

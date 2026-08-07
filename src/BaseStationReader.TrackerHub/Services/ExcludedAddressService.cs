#nullable enable

using BaseStationReader.BusinessLogic.Database;
using BaseStationReader.Data;
using BaseStationReader.Entities.Tracking;
using BaseStationReader.Interfaces.Logging;
using Microsoft.EntityFrameworkCore;

namespace BaseStationReader.TrackerHub.Services;

/// <summary>
/// Creates short-lived database managers for excluded-address UI operations.
/// </summary>
public sealed class ExcludedAddressService : IExcludedAddressService
{
    private readonly IDbContextFactory<BaseStationReaderDbContext> _contextFactory;
    private readonly ITrackerLogger _logger;

    /// <summary>
    /// Initialises a new excluded-address service.
    /// </summary>
    /// <param name="contextFactory">The database context factory.</param>
    /// <param name="logger">The application logger.</param>
    public ExcludedAddressService(
        IDbContextFactory<BaseStationReaderDbContext> contextFactory,
        ITrackerLogger logger)
    {
        _contextFactory = contextFactory;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ExcludedAddress>> SearchAsync(
        string? address,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var manager = new DatabaseManagementFactory(_logger, context, 0).ExcludedAddressManager;
        return await manager.SearchAsync(address, cancellationToken);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(string address, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var manager = new DatabaseManagementFactory(_logger, context, 0).ExcludedAddressManager;
        await manager.DeleteAsync(address, cancellationToken);
    }
}

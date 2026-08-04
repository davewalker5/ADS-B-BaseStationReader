#nullable enable

using BaseStationReader.BusinessLogic.Database;
using BaseStationReader.Data;
using BaseStationReader.Entities.Api;
using BaseStationReader.Interfaces.Logging;
using Microsoft.EntityFrameworkCore;

namespace BaseStationReader.TrackerHub.Services;

public sealed class ManufacturerReferenceService(
    IDbContextFactory<BaseStationReaderDbContext> contextFactory,
    TrackingRuntime runtime,
    ITrackerLogger logger) : IManufacturerReferenceService
{
    /// <inheritdoc />
    public async Task<List<Manufacturer>> FindAsync(
        string? name,
        CancellationToken cancellationToken = default)
    {
        var cleanName = name?.Trim().ToUpperInvariant() ?? "";
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await new DatabaseManagementFactory(logger, context, 0).ManufacturerManager
            .ListAsync(x => cleanName == "" || x.Name.ToUpper().Contains(cleanName));
    }

    /// <inheritdoc />
    public async Task<Manufacturer> SaveAsync(
        Manufacturer manufacturer,
        CancellationToken cancellationToken = default)
    {
        Manufacturer? saved = null;
        await runtime.ExecuteWhileIdleAsync(async () =>
        {
            await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
            var manager = new DatabaseManagementFactory(logger, context, 0).ManufacturerManager;
            saved = manufacturer.Id == 0
                ? await manager.AddAsync(manufacturer.Name, manufacturer.ProvenanceId)
                : await manager.UpdateAsync(manufacturer.Id, manufacturer.Name, manufacturer.ProvenanceId);
        }, cancellationToken);
        return saved!;
    }

    /// <inheritdoc />
    public Task DeleteAsync(int id, CancellationToken cancellationToken = default)
        => runtime.ExecuteWhileIdleAsync(async () =>
        {
            await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
            await new DatabaseManagementFactory(logger, context, 0).ManufacturerManager.DeleteAsync(id);
        }, cancellationToken);
}

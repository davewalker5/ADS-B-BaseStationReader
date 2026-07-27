#nullable enable

using BaseStationReader.BusinessLogic.Database;
using BaseStationReader.Data;
using BaseStationReader.Entities.Api;
using BaseStationReader.Interfaces.Logging;
using Microsoft.EntityFrameworkCore;

namespace BaseStationReader.TrackerHub.Services;

public sealed class AircraftReferenceService(
    IDbContextFactory<BaseStationReaderDbContext> contextFactory,
    TrackingRuntime runtime,
    ITrackerLogger logger) : IAircraftReferenceService
{
    public async Task<List<Aircraft>> FindAsync(
        string? address,
        string? registration,
        CancellationToken cancellationToken = default)
    {
        var cleanAddress = address?.Trim().ToUpperInvariant() ?? "";
        var cleanRegistration = registration?.Trim().ToUpperInvariant() ?? "";
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var manager = new DatabaseManagementFactory(logger, context, 0).AircraftManager;
        return await manager.ListAsync(x =>
            (cleanAddress == "" || x.Address.ToUpper().Contains(cleanAddress)) &&
            (cleanRegistration == "" || x.Registration.ToUpper().Contains(cleanRegistration)));
    }

    public async Task<List<Model>> ListModelsAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await new DatabaseManagementFactory(logger, context, 0).ModelManager.ListAsync(x => true);
    }

    public async Task<Aircraft> SaveAsync(Aircraft aircraft, CancellationToken cancellationToken = default)
    {
        Aircraft? saved = null;
        await runtime.ExecuteWhileIdleAsync(async () =>
        {
            await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
            var manager = new DatabaseManagementFactory(logger, context, 0).AircraftManager;
            saved = aircraft.Id == 0
                ? await manager.AddAsync(
                    aircraft.Address.Trim().ToUpperInvariant(),
                    aircraft.Registration.Trim().ToUpperInvariant(),
                    aircraft.Manufactured,
                    aircraft.Age,
                    aircraft.ModelId,
                    aircraft.ProvenanceId)
                : await manager.UpdateAsync(
                    aircraft.Id,
                    aircraft.Address.Trim().ToUpperInvariant(),
                    aircraft.Registration.Trim().ToUpperInvariant(),
                    aircraft.Manufactured,
                    aircraft.Age,
                    aircraft.ModelId,
                    aircraft.ProvenanceId);
        }, cancellationToken);
        return saved!;
    }

    public Task DeleteAsync(int id, CancellationToken cancellationToken = default)
        => runtime.ExecuteWhileIdleAsync(async () =>
        {
            await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
            await new DatabaseManagementFactory(logger, context, 0).AircraftManager.DeleteAsync(id);
        }, cancellationToken);
}

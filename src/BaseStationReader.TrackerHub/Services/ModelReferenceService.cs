#nullable enable

using BaseStationReader.BusinessLogic.Database;
using BaseStationReader.Data;
using BaseStationReader.Entities.Api;
using BaseStationReader.Interfaces.Logging;
using Microsoft.EntityFrameworkCore;

namespace BaseStationReader.TrackerHub.Services;

public sealed class ModelReferenceService(
    IDbContextFactory<BaseStationReaderDbContext> contextFactory,
    TrackingRuntime runtime,
    ITrackerLogger logger) : IModelReferenceService
{
    public async Task<List<Manufacturer>> ListManufacturersAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await new DatabaseManagementFactory(logger, context, 0).ManufacturerManager.ListAsync(x => true);
    }

    public async Task<List<Model>> FindAsync(
        string? manufacturerName,
        string? modelName,
        string? modelIcao,
        CancellationToken cancellationToken = default)
    {
        var cleanManufacturer = manufacturerName?.Trim().ToUpperInvariant() ?? "";
        var cleanName = modelName?.Trim().ToUpperInvariant() ?? "";
        var cleanIcao = modelIcao?.Trim().ToUpperInvariant() ?? "";
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await new DatabaseManagementFactory(logger, context, 0).ModelManager
            .ListAsync(x =>
                (cleanManufacturer == "" || x.Manufacturer.Name.ToUpper().Contains(cleanManufacturer)) &&
                (cleanName == "" || x.Name.ToUpper().Contains(cleanName)) &&
                (cleanIcao == "" || (x.ICAO != null && x.ICAO.ToUpper().Contains(cleanIcao))));
    }

    public async Task<Model> SaveAsync(Model model, CancellationToken cancellationToken = default)
    {
        Model? saved = null;
        await runtime.ExecuteWhileIdleAsync(async () =>
        {
            await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
            var manager = new DatabaseManagementFactory(logger, context, 0).ModelManager;
            saved = model.Id == 0
                ? await manager.AddAsync(
                    model.IATA, model.ICAO, model.Name, model.ManufacturerId, model.ProvenanceId)
                : await manager.UpdateAsync(
                    model.Id, model.IATA, model.ICAO, model.Name,
                    model.ManufacturerId, model.ProvenanceId);
        }, cancellationToken);
        return saved!;
    }

    public Task DeleteAsync(int id, CancellationToken cancellationToken = default)
        => runtime.ExecuteWhileIdleAsync(async () =>
        {
            await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
            await new DatabaseManagementFactory(logger, context, 0).ModelManager.DeleteAsync(id);
        }, cancellationToken);
}

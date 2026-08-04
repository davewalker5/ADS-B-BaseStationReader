#nullable enable

using BaseStationReader.BusinessLogic.Database;
using BaseStationReader.Data;
using BaseStationReader.Entities.Api;
using BaseStationReader.Interfaces.Logging;
using Microsoft.EntityFrameworkCore;

namespace BaseStationReader.TrackerHub.Services;

public sealed class AirlineReferenceService(
    IDbContextFactory<BaseStationReaderDbContext> contextFactory,
    TrackingRuntime runtime,
    ITrackerLogger logger) : IAirlineReferenceService
{
    /// <inheritdoc />
    public async Task<List<Airline>> FindAsync(
        string? iata,
        string? icao,
        string? name,
        CancellationToken cancellationToken = default)
    {
        var cleanIata = iata?.Trim().ToUpperInvariant() ?? "";
        var cleanIcao = icao?.Trim().ToUpperInvariant() ?? "";
        var cleanName = name?.Trim().ToUpperInvariant() ?? "";
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await new DatabaseManagementFactory(logger, context, 0).AirlineManager.ListAsync(x =>
            (cleanIata == "" || x.IATA.ToUpper().Contains(cleanIata)) &&
            (cleanIcao == "" || x.ICAO.ToUpper().Contains(cleanIcao)) &&
            (cleanName == "" || x.Name.ToUpper().Contains(cleanName)));
    }

    /// <inheritdoc />
    public async Task<Airline> SaveAsync(Airline airline, CancellationToken cancellationToken = default)
    {
        Airline? saved = null;
        await runtime.ExecuteWhileIdleAsync(async () =>
        {
            await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
            var manager = new DatabaseManagementFactory(logger, context, 0).AirlineManager;
            saved = airline.Id == 0
                ? await manager.AddAsync(airline.IATA, airline.ICAO, airline.Name, airline.ProvenanceId)
                : await manager.UpdateAsync(airline.Id, airline.IATA, airline.ICAO, airline.Name, airline.ProvenanceId);
        }, cancellationToken);
        return saved!;
    }

    /// <inheritdoc />
    public Task DeleteAsync(int id, CancellationToken cancellationToken = default)
        => runtime.ExecuteWhileIdleAsync(async () =>
        {
            await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
            await new DatabaseManagementFactory(logger, context, 0).AirlineManager.DeleteAsync(id);
        }, cancellationToken);
}

#nullable enable

using BaseStationReader.BusinessLogic.Database;
using BaseStationReader.Data;
using BaseStationReader.Entities.Api;
using BaseStationReader.Interfaces.Logging;
using Microsoft.EntityFrameworkCore;

namespace BaseStationReader.TrackerHub.Services;

public sealed class AirportReferenceService(
    IDbContextFactory<BaseStationReaderDbContext> contextFactory,
    TrackingRuntime runtime,
    ITrackerLogger logger) : IAirportReferenceService
{
    /// <inheritdoc />
    public async Task<List<Airport>> FindAsync(
        string? iata,
        string? icao,
        string? name,
        CancellationToken cancellationToken = default)
    {
        var cleanIata = iata?.Trim().ToUpperInvariant() ?? "";
        var cleanIcao = icao?.Trim().ToUpperInvariant() ?? "";
        var cleanName = name?.Trim().ToUpperInvariant() ?? "";
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await new DatabaseManagementFactory(logger, context, 0).AirportManager.ListAsync(x =>
            (cleanIata == "" || x.IATA.ToUpper().Contains(cleanIata)) &&
            (cleanIcao == "" || x.ICAO.ToUpper().Contains(cleanIcao)) &&
            (cleanName == "" || x.Name.ToUpper().Contains(cleanName)));
    }

    /// <inheritdoc />
    public async Task<Airport> SaveAsync(Airport airport, CancellationToken cancellationToken = default)
    {
        Airport? saved = null;
        await runtime.ExecuteWhileIdleAsync(async () =>
        {
            await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
            var manager = new DatabaseManagementFactory(logger, context, 0).AirportManager;
            saved = airport.Id == 0
                ? await manager.AddAsync(Copy(airport))
                : await manager.UpdateAsync(Copy(airport));
        }, cancellationToken);
        return saved!;
    }

    /// <inheritdoc />
    public Task DeleteAsync(int id, CancellationToken cancellationToken = default)
        => runtime.ExecuteWhileIdleAsync(async () =>
        {
            await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
            await new DatabaseManagementFactory(logger, context, 0).AirportManager.DeleteAsync(id);
        }, cancellationToken);

    private static Airport Copy(Airport source) => new()
    {
        Id = source.Id,
        IATA = source.IATA,
        ICAO = source.ICAO,
        Name = source.Name,
        Latitude = source.Latitude,
        Longitude = source.Longitude,
        ProvenanceId = source.ProvenanceId
    };
}

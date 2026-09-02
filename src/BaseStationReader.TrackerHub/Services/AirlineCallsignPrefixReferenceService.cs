#nullable enable

using BaseStationReader.BusinessLogic.Database;
using BaseStationReader.Data;
using BaseStationReader.Entities.Api;
using BaseStationReader.Interfaces.Logging;
using Microsoft.EntityFrameworkCore;

namespace BaseStationReader.TrackerHub.Services;

public sealed class AirlineCallsignPrefixReferenceService(
    IDbContextFactory<BaseStationReaderDbContext> contextFactory,
    TrackingRuntime runtime,
    ITrackerLogger logger) : IAirlineCallsignPrefixReferenceService
{
    /// <inheritdoc />
    public async Task<List<AirlineCallsignPrefix>> FindAsync(
        string? prefix,
        string? airlineIcao,
        string? airlineName,
        CancellationToken cancellationToken = default)
    {
        var cleanPrefix = prefix?.Trim().ToUpperInvariant() ?? "";
        var cleanIcao = airlineIcao?.Trim().ToUpperInvariant() ?? "";
        var cleanName = airlineName?.Trim().ToUpperInvariant() ?? "";
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await new DatabaseManagementFactory(logger, context, 0)
            .AirlineCallsignPrefixManager.ListAsync(x =>
                (cleanPrefix == "" || x.Prefix.ToUpper().Contains(cleanPrefix)) &&
                (cleanIcao == "" || x.Airline.ICAO.ToUpper().Contains(cleanIcao)) &&
                (cleanName == "" || x.Airline.Name.ToUpper().Contains(cleanName)));
    }

    /// <inheritdoc />
    public async Task<AirlineCallsignPrefix> SaveAsync(
        AirlineCallsignPrefix mapping,
        CancellationToken cancellationToken = default)
    {
        AirlineCallsignPrefix? saved = null;
        await runtime.ExecuteWhileIdleAsync(async () =>
        {
            await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
            var manager = new DatabaseManagementFactory(logger, context, 0).AirlineCallsignPrefixManager;
            saved = mapping.Id == 0
                ? await manager.AddAsync(mapping.Prefix, mapping.AirlineId, mapping.ProvenanceId)
                : await manager.UpdateAsync(
                    mapping.Id, mapping.Prefix, mapping.AirlineId, mapping.ProvenanceId);
        }, cancellationToken);
        return saved!;
    }

    /// <inheritdoc />
    public Task DeleteAsync(int id, CancellationToken cancellationToken = default)
        => runtime.ExecuteWhileIdleAsync(async () =>
        {
            await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
            await new DatabaseManagementFactory(logger, context, 0)
                .AirlineCallsignPrefixManager.DeleteAsync(id);
        }, cancellationToken);
}

#nullable enable

using BaseStationReader.BusinessLogic.Database;
using BaseStationReader.Data;
using BaseStationReader.Entities.Api;
using BaseStationReader.Interfaces.Logging;
using Microsoft.EntityFrameworkCore;

namespace BaseStationReader.TrackerHub.Services;

public sealed class FlightReferenceService(
    IDbContextFactory<BaseStationReaderDbContext> contextFactory,
    TrackingRuntime runtime,
    ITrackerLogger logger) : IFlightReferenceService
{
    public async Task<List<Flight>> FindAsync(
        string? callsign,
        string? iata,
        string? icao,
        CancellationToken cancellationToken = default)
    {
        var cleanCallsign = callsign?.Trim().ToUpperInvariant() ?? "";
        var cleanIata = iata?.Trim().ToUpperInvariant() ?? "";
        var cleanIcao = icao?.Trim().ToUpperInvariant() ?? "";
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await new DatabaseManagementFactory(logger, context, 0).FlightManager.ListAsync(x =>
            (cleanCallsign == "" || x.Callsign.ToUpper().Contains(cleanCallsign)) &&
            (cleanIata == "" || x.IATA.ToUpper().Contains(cleanIata)) &&
            (cleanIcao == "" || x.ICAO.ToUpper().Contains(cleanIcao)));
    }

    public async Task<List<Airline>> ListAirlinesAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await new DatabaseManagementFactory(logger, context, 0).AirlineManager.ListAsync(x => true);
    }

    public async Task<List<Airport>> ListAirportsAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await new DatabaseManagementFactory(logger, context, 0).AirportManager.ListAsync(x => true);
    }

    public async Task<Flight> SaveAsync(Flight flight, CancellationToken cancellationToken = default)
    {
        Flight? saved = null;
        await runtime.ExecuteWhileIdleAsync(async () =>
        {
            await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
            var manager = new DatabaseManagementFactory(logger, context, 0).FlightManager;
            saved = flight.Id == 0
                ? await manager.AddAsync(
                    flight.IATA, flight.ICAO, flight.Callsign, flight.AirlineId,
                    flight.OriginAirportId, flight.DestinationAirportId, flight.ProvenanceId)
                : await manager.UpdateAsync(
                    flight.Id, flight.IATA, flight.ICAO, flight.Callsign, flight.AirlineId,
                    flight.OriginAirportId, flight.DestinationAirportId, flight.ProvenanceId);
        }, cancellationToken);
        return saved!;
    }

    public Task DeleteAsync(int id, CancellationToken cancellationToken = default)
        => runtime.ExecuteWhileIdleAsync(async () =>
        {
            await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
            await new DatabaseManagementFactory(logger, context, 0).FlightManager.DeleteAsync(id);
        }, cancellationToken);
}

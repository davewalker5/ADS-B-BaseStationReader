#nullable enable

using BaseStationReader.Entities.Api;

namespace BaseStationReader.TrackerHub.Services;

public interface IFlightReferenceService
{
    Task<List<Flight>> FindAsync(string? callsign, string? iata, string? icao, CancellationToken cancellationToken = default);
    Task<List<Airline>> ListAirlinesAsync(CancellationToken cancellationToken = default);
    Task<List<Airport>> ListAirportsAsync(CancellationToken cancellationToken = default);
    Task<Flight> SaveAsync(Flight flight, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}

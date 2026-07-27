#nullable enable

using BaseStationReader.Entities.Api;

namespace BaseStationReader.TrackerHub.Services;

public interface IAirlineReferenceService
{
    Task<List<Airline>> FindAsync(string? iata, string? icao, string? name, CancellationToken cancellationToken = default);
    Task<Airline> SaveAsync(Airline airline, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}

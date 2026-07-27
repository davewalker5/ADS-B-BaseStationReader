#nullable enable

using BaseStationReader.Entities.Api;

namespace BaseStationReader.TrackerHub.Services;

public interface IAirportReferenceService
{
    Task<List<Airport>> FindAsync(string? iata, string? icao, string? name, CancellationToken cancellationToken = default);
    Task<Airport> SaveAsync(Airport airport, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}

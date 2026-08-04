#nullable enable

using BaseStationReader.Entities.Api;

namespace BaseStationReader.TrackerHub.Services;

public interface IAirportReferenceService
{
    /// <summary>Finds airports matching the supplied codes and name.</summary>
    Task<List<Airport>> FindAsync(string? iata, string? icao, string? name, CancellationToken cancellationToken = default);
    /// <summary>Adds or updates an airport.</summary>
    Task<Airport> SaveAsync(Airport airport, CancellationToken cancellationToken = default);
    /// <summary>Deletes an airport.</summary>
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}

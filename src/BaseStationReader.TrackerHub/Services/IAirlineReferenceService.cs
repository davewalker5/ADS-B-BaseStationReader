#nullable enable

using BaseStationReader.Entities.Api;

namespace BaseStationReader.TrackerHub.Services;

public interface IAirlineReferenceService
{
    /// <summary>Finds airlines matching the supplied codes and name.</summary>
    Task<List<Airline>> FindAsync(string? iata, string? icao, string? name, CancellationToken cancellationToken = default);
    /// <summary>Adds or updates an airline.</summary>
    Task<Airline> SaveAsync(Airline airline, CancellationToken cancellationToken = default);
    /// <summary>Deletes an airline.</summary>
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}

#nullable enable

using BaseStationReader.Entities.Api;

namespace BaseStationReader.TrackerHub.Services;

public interface IFlightReferenceService
{
    /// <summary>Finds flights matching the supplied callsign and codes.</summary>
    Task<List<Flight>> FindAsync(string? callsign, string? iata, string? icao, CancellationToken cancellationToken = default);
    /// <summary>Lists airlines available for flight editing.</summary>
    Task<List<Airline>> ListAirlinesAsync(CancellationToken cancellationToken = default);
    /// <summary>Lists airports available for flight editing.</summary>
    Task<List<Airport>> ListAirportsAsync(CancellationToken cancellationToken = default);
    /// <summary>Adds or updates a flight.</summary>
    Task<Flight> SaveAsync(Flight flight, CancellationToken cancellationToken = default);
    /// <summary>Deletes a flight.</summary>
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}

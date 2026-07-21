using BaseStationReader.Entities.Api;
using BaseStationReader.Entities.Config;
using BaseStationReader.TrackerHub.Models;

namespace BaseStationReader.TrackerHub.Services;

/// <summary>
/// Provides configured airport schedule services, retrieval, and mapping persistence.
/// </summary>
public interface IAirportScheduleLookupService
{
    /// <summary>
    /// Returns airports available for selection on the schedule page.
    /// </summary>
    Task<IReadOnlyList<AirportWeatherOption>> GetAirportsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns configured API services that provide schedule retrieval.
    /// </summary>
    IReadOnlyList<ApiServiceType> GetServices();

    /// <summary>
    /// Returns today's default schedule range using the configured times.
    /// </summary>
    (DateTime From, DateTime To) GetDefaultRange(DateTime today);

    /// <summary>
    /// Retrieves flight mappings for an airport and time range.
    /// </summary>
    Task<IReadOnlyList<FlightIATACodeMapping>> LookupAsync(
        ApiServiceType serviceType,
        string iata,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds or updates retrieved flight mappings in the tracking database.
    /// </summary>
    Task SaveAsync(
        IEnumerable<FlightIATACodeMapping> mappings,
        CancellationToken cancellationToken = default);
}

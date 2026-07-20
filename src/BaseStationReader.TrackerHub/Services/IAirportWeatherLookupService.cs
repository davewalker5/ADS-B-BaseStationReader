using BaseStationReader.Entities.Config;
using BaseStationReader.TrackerHub.Models;

namespace BaseStationReader.TrackerHub.Services;

/// <summary>
/// Provides configured airport weather services and executes weather lookups.
/// </summary>
public interface IAirportWeatherLookupService
{
    /// <summary>
    /// Returns airports available for selection on the weather page.
    /// </summary>
    /// <param name="cancellationToken">Cancels the database query.</param>
    /// <returns>Airports ordered by name and code.</returns>
    Task<IReadOnlyList<AirportWeatherOption>> GetAirportsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns configured API services that provide the requested weather endpoint.
    /// </summary>
    /// <param name="endpointType">The METAR or TAF endpoint type.</param>
    /// <returns>The matching API services.</returns>
    IReadOnlyList<ApiServiceType> GetServices(ApiEndpointType endpointType);

    /// <summary>
    /// Looks up and decodes airport weather using a selected configured service.
    /// </summary>
    /// <param name="endpointType">The METAR or TAF endpoint type.</param>
    /// <param name="serviceType">The selected API service.</param>
    /// <param name="icao">The four-letter airport ICAO code.</param>
    /// <param name="cancellationToken">Cancels the lookup before it starts.</param>
    /// <returns>The raw and decoded reports returned by the API.</returns>
    Task<IReadOnlyList<AirportWeatherReport>> LookupAsync(
        ApiEndpointType endpointType,
        ApiServiceType serviceType,
        string icao,
        CancellationToken cancellationToken = default);
}

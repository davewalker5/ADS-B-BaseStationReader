using BaseStationReader.Entities.Config;
using BaseStationReader.TrackerHub.Models;

namespace BaseStationReader.TrackerHub.Services;

/// <summary>
/// Provides configured aircraft and flight services and executes interactive lookups.
/// </summary>
public interface IReferenceLookupService
{
    /// <summary>
    /// Returns configured API services that provide the requested endpoint.
    /// </summary>
    /// <param name="endpointType">The Aircraft or Flights endpoint type.</param>
    /// <returns>The matching configured services.</returns>
    IReadOnlyList<ApiServiceType> GetServices(ApiEndpointType endpointType);

    /// <summary>
    /// Resolves aircraft and flight details using the selected services.
    /// </summary>
    /// <param name="aircraftService">The service used if aircraft data is not available locally.</param>
    /// <param name="flightService">The service used if flight data is not available locally.</param>
    /// <param name="address">The optional aircraft ICAO address.</param>
    /// <param name="callsign">The optional observed flight callsign.</param>
    /// <param name="cancellationToken">Cancels the operation before a lookup begins.</param>
    /// <returns>The resolved aircraft and flight details.</returns>
    Task<ReferenceLookupResult> LookupAsync(
        ApiServiceType aircraftService,
        ApiServiceType flightService,
        string address,
        string callsign,
        CancellationToken cancellationToken = default);
}

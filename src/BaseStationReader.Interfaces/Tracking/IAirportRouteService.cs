using BaseStationReader.Entities.Tracking;

namespace BaseStationReader.Interfaces.Tracking;

/// <summary>
/// Resolves airport codes and prepares direct-route map geometry.
/// </summary>
public interface IAirportRouteService
{
    /// <summary>
    /// Resolves two airports and builds renderer-neutral great-circle route geometry.
    /// </summary>
    /// <param name="originIata">The origin airport's three-letter IATA code.</param>
    /// <param name="destinationIata">The destination airport's three-letter IATA code.</param>
    /// <param name="cancellationToken">Cancels the airport lookup.</param>
    /// <returns>The resolved endpoints, sampled route, distance, and geographic bounds.</returns>
    Task<RoutePlotDto> BuildRouteAsync(
        string originIata,
        string destinationIata,
        CancellationToken cancellationToken = default);
}

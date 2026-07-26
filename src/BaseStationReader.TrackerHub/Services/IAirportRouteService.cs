using BaseStationReader.TrackerHub.Models;

namespace BaseStationReader.TrackerHub.Services;

/// <summary>
/// Resolves airport codes and prepares direct-route map geometry.
/// </summary>
public interface IAirportRouteService
{
    Task<RoutePlotDto> BuildRouteAsync(
        string originIata,
        string destinationIata,
        CancellationToken cancellationToken = default);
}

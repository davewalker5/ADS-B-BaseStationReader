#nullable enable

namespace BaseStationReader.TrackerHub.Services;

/// <summary>
/// Retrieves an optional Mapbox static image for a bounded flight-path ground plane.
/// </summary>
public interface IMapboxStaticMapService
{
    bool IsConfigured { get; }

    /// <summary>
    /// Retrieves a static ground image covering the supplied geographic bounds.
    /// </summary>
    /// <param name="north">Northern latitude.</param>
    /// <param name="south">Southern latitude.</param>
    /// <param name="east">Eastern longitude.</param>
    /// <param name="west">Western longitude.</param>
    /// <param name="cancellationToken">Cancels the remote request.</param>
    /// <returns>PNG bytes, or <see langword="null"/> when Mapbox is unavailable.</returns>
    Task<byte[]?> GetMapAsync(
        double north,
        double south,
        double east,
        double west,
        CancellationToken cancellationToken = default);
}

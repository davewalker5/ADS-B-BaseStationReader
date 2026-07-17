#nullable enable

namespace BaseStationReader.TrackerHub.Services;

/// <summary>
/// Implements the notebook-compatible Mapbox Static Images API adapter.
/// </summary>
public sealed class MapboxStaticMapService : IMapboxStaticMapService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<MapboxStaticMapService> _logger;
    private readonly string _accessToken;
    private readonly string _style;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_accessToken);

    /// <summary>
    /// Initialises the adapter from the environment-aware Tracker Hub configuration.
    /// </summary>
    /// <param name="httpClient">HTTP client used only for Mapbox image requests.</param>
    /// <param name="configuration">Tracker Hub configuration.</param>
    /// <param name="logger">Application logger.</param>
    public MapboxStaticMapService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<MapboxStaticMapService> logger)
    {
        // An empty token deliberately disables ground retrieval without disabling path rendering.
        _httpClient = httpClient;
        _logger = logger;
        _accessToken = configuration["Mapbox:AccessToken"] ?? string.Empty;
        _style = configuration["Mapbox:Style"] ?? "streets-v12";
    }

    /// <inheritdoc />
    public async Task<byte[]?> GetMapAsync(
        double north,
        double south,
        double east,
        double west,
        CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            // Missing configuration is a supported blank-ground mode, not an application error.
            return null;
        }

        var style = Uri.EscapeDataString(_style);
        var token = Uri.EscapeDataString(_accessToken);
        var bounds = FormattableString.Invariant($"[{west},{south},{east},{north}]");
        // Match the notebook's high-DPI source so labels survive conversion into the 3D floor mesh.
        var requestUri = $"https://api.mapbox.com/styles/v1/mapbox/{style}/static/{bounds}/1024x1024@2x?access_token={token}";

        try
        {
            // Buffer the image on the server so the configured token is not sent to browser JavaScript.
            using var response = await _httpClient.GetAsync(requestUri, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Mapbox ground request returned status {StatusCode}", response.StatusCode);
                return null;
            }

            return await response.Content.ReadAsByteArrayAsync(cancellationToken);
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            // A remote map failure must leave the ground blank while preserving the local flight path.
            _logger.LogWarning(exception, "Mapbox ground request failed");
            return null;
        }
    }
}

using BaseStationReader.Api;
using BaseStationReader.Api.Wrapper;
using BaseStationReader.BusinessLogic.Database;
using BaseStationReader.BusinessLogic.Weather;
using BaseStationReader.Data;
using BaseStationReader.Entities.Config;
using BaseStationReader.Interfaces.Api;
using BaseStationReader.Interfaces.Logging;
using BaseStationReader.TrackerHub.Models;
using Microsoft.EntityFrameworkCore;

namespace BaseStationReader.TrackerHub.Services;

/// <summary>
/// Executes configured METAR and TAF API lookups for the integrated web UI.
/// </summary>
public sealed class AirportWeatherLookupService : IAirportWeatherLookupService
{
    private static readonly TimeSpan CacheLifetime = TimeSpan.FromMinutes(5);
    private readonly ExternalApiSettings _settings;
    private readonly IDbContextFactory<BaseStationReaderDbContext> _contextFactory;
    private readonly ITrackerLogger _logger;
    private readonly ITransientResponseCache _cache;
    private readonly IExternalApiFactory _apiFactory = new ExternalApiFactory();

    /// <summary>
    /// Initialises an airport weather lookup service.
    /// </summary>
    /// <param name="settings">The external API settings bound from appsettings.json.</param>
    /// <param name="contextFactory">The factory used to create a context for API-call logging.</param>
    /// <param name="logger">The application logger.</param>
    /// <param name="cache">The process-memory-only transient response cache.</param>
    public AirportWeatherLookupService(
        ExternalApiSettings settings,
        IDbContextFactory<BaseStationReaderDbContext> contextFactory,
        ITrackerLogger logger,
        ITransientResponseCache cache)
    {
        _settings = settings;
        _contextFactory = contextFactory;
        _logger = logger;
        _cache = cache;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AirportWeatherOption>> GetAirportsAsync(
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        // Project only the values required by the selector and avoid tracking read-only reference data.
        return await context.Airports
            .AsNoTracking()
            .OrderBy(airport => airport.Name)
            .ThenBy(airport => airport.IATA)
            .ThenBy(airport => airport.ICAO)
            .Select(airport => new AirportWeatherOption(airport.Name, airport.IATA, airport.ICAO))
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public IReadOnlyList<ApiServiceType> GetServices(ApiEndpointType endpointType)
    {
        // Only METAR and TAF are meaningful choices for this feature.
        if (endpointType is not (ApiEndpointType.METAR or ApiEndpointType.TAF)) return [];

        // Configuration is the source of truth, so services without the selected endpoint are hidden.
        return _settings.ApiServices
            .Where(service => service.ApiEndpoints?.Any(endpoint => endpoint.EndpointType == endpointType) == true)
            .Select(service => service.Service)
            .Distinct()
            .OrderBy(service => service.ToString(), StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AirportWeatherReport>> LookupAsync(
        ApiEndpointType endpointType,
        ApiServiceType serviceType,
        string icao,
        CancellationToken cancellationToken = default)
    {
        // Validate all UI-provided values again at the service boundary.
        if (endpointType is not (ApiEndpointType.METAR or ApiEndpointType.TAF))
            throw new ArgumentException("Select current weather or weather forecast.", nameof(endpointType));
        if (!GetServices(endpointType).Contains(serviceType))
            throw new ArgumentException($"{serviceType} is not configured for {endpointType} lookups.", nameof(serviceType));

        var normalisedIcao = (icao ?? string.Empty).Trim().ToUpperInvariant();
        if (normalisedIcao.Length != 4 || !normalisedIcao.All(character => character is >= 'A' and <= 'Z'))
            throw new ArgumentException("Enter a four-letter airport ICAO code.", nameof(icao));

        // Weather expires quickly, and the cache retains objects in process memory only—never in a file or database.
        var cacheKey = $"weather:{endpointType}:{serviceType}:{normalisedIcao}";
        return await _cache.GetOrCreateAsync(
            cacheKey,
            CacheLifetime,
            token => LookupUncachedAsync(endpointType, serviceType, normalisedIcao, token),
            cancellationToken);
    }

    /// <summary>
    /// Executes and decodes one uncached weather API request.
    /// </summary>
    private async Task<IReadOnlyList<AirportWeatherReport>> LookupUncachedAsync(
        ApiEndpointType endpointType,
        ApiServiceType serviceType,
        string normalisedIcao,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        // Keep the context alive for API usage logging until the remote request has completed.
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var databaseFactory = new DatabaseManagementFactory(_logger, context, 0);
        var wrapper = _apiFactory.GetWrapperInstance(
            TrackerHttpClient.Instance,
            databaseFactory,
            serviceType,
            _settings);

        // Call only the endpoint selected by the user.
        var reports = endpointType == ApiEndpointType.METAR
            ? await wrapper.LookupCurrentAirportWeatherAsync(normalisedIcao)
            : await wrapper.LookupAirportWeatherForecastAsync(normalisedIcao);

        if (reports is null) return [];

        // Materialise the API response once and pair each raw report with its decoded output.
        return reports
            .Where(report => !string.IsNullOrWhiteSpace(report))
            .Select(report => new AirportWeatherReport(report, Decode(endpointType, report)))
            .ToList();
    }

    /// <summary>
    /// Decodes one weather report while preserving unusual provider output.
    /// </summary>
    /// <param name="endpointType">The selected METAR or TAF endpoint.</param>
    /// <param name="report">The raw API report.</param>
    /// <returns>The decoded lines, or an explanatory line when decoding is unavailable.</returns>
    private static IReadOnlyList<string> Decode(ApiEndpointType endpointType, string report)
    {
        try
        {
            // Use the explicit decoder because the selected endpoint already establishes the report type.
            return endpointType == ApiEndpointType.METAR
                ? MetarDecoder.Decode(report)
                : TafDecoder.Decode(report);
        }
        catch (ArgumentException exception)
        {
            // The raw response remains useful even when a provider returns a non-standard report.
            return [$"Decoded output unavailable: {exception.Message}"];
        }
    }
}

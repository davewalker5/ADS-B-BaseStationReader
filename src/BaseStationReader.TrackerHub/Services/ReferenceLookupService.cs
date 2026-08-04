using BaseStationReader.Api;
using BaseStationReader.Api.Wrapper;
using BaseStationReader.BusinessLogic.Database;
using BaseStationReader.Data;
using BaseStationReader.Entities.Config;
using BaseStationReader.Interfaces.Api;
using BaseStationReader.Interfaces.Logging;
using BaseStationReader.TrackerHub.Models;
using Microsoft.EntityFrameworkCore;

namespace BaseStationReader.TrackerHub.Services;

/// <summary>
/// Executes local-first aircraft and flight lookups for the integrated web UI.
/// </summary>
public sealed class ReferenceLookupService : IReferenceLookupService
{
    private static readonly TimeSpan CacheLifetime = TimeSpan.FromHours(1);
    private readonly ExternalApiSettings _settings;
    private readonly IDbContextFactory<BaseStationReaderDbContext> _contextFactory;
    private readonly ITrackerLogger _logger;
    private readonly ITransientResponseCache _cache;
    private readonly IExternalApiFactory _apiFactory = new ExternalApiFactory();

    /// <summary>
    /// Initialises the interactive reference lookup service.
    /// </summary>
    /// <param name="settings">The configured external API services.</param>
    /// <param name="contextFactory">The database context factory.</param>
    /// <param name="logger">The application logger.</param>
    /// <param name="cache">The process-memory-only transient response cache.</param>
    public ReferenceLookupService(
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
    public IReadOnlyList<ApiServiceType> GetServices(ApiEndpointType endpointType)
    {
        if (endpointType is not (ApiEndpointType.Aircraft or ApiEndpointType.Flights))
        {
            return [];
        }

        return _settings.ApiServices
            .Where(service => service.ApiEndpoints?.Any(endpoint => endpoint.EndpointType == endpointType) == true)
            .Select(service => service.Service)
            .Distinct()
            .OrderBy(service => service.ToString(), StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <inheritdoc />
    public async Task<ReferenceLookupResult> LookupAsync(
        ApiServiceType aircraftService,
        ApiServiceType flightService,
        string address,
        string callsign,
        CancellationToken cancellationToken = default)
    {
        var normalisedAddress = (address ?? string.Empty).Trim().ToUpperInvariant();
        var normalisedCallsign = (callsign ?? string.Empty).Trim().ToUpperInvariant();
        var lookupAircraft = normalisedAddress.Length > 0;
        var lookupFlight = normalisedCallsign.Length > 0;

        if (!lookupAircraft && !lookupFlight)
        {
            throw new ArgumentException("Enter an aircraft address, a flight callsign, or both.");
        }
        if (lookupAircraft && (normalisedAddress.Length != 6 || !normalisedAddress.All(Uri.IsHexDigit)))
        {
            throw new ArgumentException("Enter a six-character hexadecimal aircraft address.", nameof(address));
        }
        if (lookupAircraft && !GetServices(ApiEndpointType.Aircraft).Contains(aircraftService))
        {
            throw new ArgumentException($"{aircraftService} is not configured for aircraft lookups.", nameof(aircraftService));
        }
        if (lookupFlight && !GetServices(ApiEndpointType.Flights).Contains(flightService))
        {
            throw new ArgumentException($"{flightService} is not configured for flight lookups.", nameof(flightService));
        }

        // The normalized inputs identify equivalent requests without serializing or persisting any response data.
        var cacheKey = $"reference:{aircraftService}:{flightService}:{normalisedAddress}:{normalisedCallsign}";
        return await _cache.GetOrCreateAsync(
            cacheKey,
            CacheLifetime,
            token => LookupUncachedAsync(
                aircraftService,
                flightService,
                normalisedAddress,
                normalisedCallsign,
                lookupAircraft,
                lookupFlight,
                token),
            cancellationToken);
    }

    /// <summary>
    /// Executes one uncached local-first reference lookup.
    /// </summary>
    private async Task<ReferenceLookupResult> LookupUncachedAsync(
        ApiServiceType aircraftService,
        ApiServiceType flightService,
        string normalisedAddress,
        string normalisedCallsign,
        bool lookupAircraft,
        bool lookupFlight,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var databaseFactory = new DatabaseManagementFactory(_logger, context, 0);

        BaseStationReader.Entities.Api.Aircraft aircraft = null;
        BaseStationReader.Entities.Api.Flight flight = null;
        // Resolve local existence through the business-logic managers before consulting external providers.
        var aircraftIsLocal = lookupAircraft &&
            await databaseFactory.AircraftManager.GetAsync(item => item.Address == normalisedAddress) is not null;
        cancellationToken.ThrowIfCancellationRequested();
        var flightIsLocal = lookupFlight &&
            await databaseFactory.FlightManager.GetAsync(item => item.Callsign == normalisedCallsign) is not null;
        cancellationToken.ThrowIfCancellationRequested();

        // Separate wrappers allow each requested lookup type to use its independently selected provider.
        if (lookupAircraft)
        {
            var aircraftWrapper = _apiFactory.GetWrapperInstance(
                TrackerHttpClient.Instance, databaseFactory, aircraftService, _settings);
            aircraft = await aircraftWrapper.LookupAircraftAsync(normalisedAddress);
        }

        if (lookupFlight)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var flightWrapper = _apiFactory.GetWrapperInstance(
                TrackerHttpClient.Instance, databaseFactory, flightService, _settings);
            flight = await flightWrapper.LookupFlightAsync(normalisedAddress, normalisedCallsign);
        }

        return new ReferenceLookupResult(
            aircraft,
            flight,
            aircraft is null ? null : aircraftIsLocal ? ReferenceLookupSource.LocalDatabase : ReferenceLookupSource.Api,
            flight is null ? null : flightIsLocal ? ReferenceLookupSource.LocalDatabase : ReferenceLookupSource.Api);
    }
}

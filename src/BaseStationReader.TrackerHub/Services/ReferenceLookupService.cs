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
    private readonly ExternalApiSettings _settings;
    private readonly IDbContextFactory<BaseStationReaderDbContext> _contextFactory;
    private readonly ITrackerLogger _logger;
    private readonly IExternalApiFactory _apiFactory = new ExternalApiFactory();

    /// <summary>
    /// Initialises the interactive reference lookup service.
    /// </summary>
    /// <param name="settings">The configured external API services.</param>
    /// <param name="contextFactory">The database context factory.</param>
    /// <param name="logger">The application logger.</param>
    public ReferenceLookupService(
        ExternalApiSettings settings,
        IDbContextFactory<BaseStationReaderDbContext> contextFactory,
        ITrackerLogger logger)
    {
        _settings = settings;
        _contextFactory = contextFactory;
        _logger = logger;
    }

    /// <inheritdoc />
    public IReadOnlyList<ApiServiceType> GetServices(ApiEndpointType endpointType)
    {
        if (endpointType is not (ApiEndpointType.Aircraft or ApiEndpointType.Flights)) return [];

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
            throw new ArgumentException("Enter an aircraft address, a flight callsign, or both.");
        if (lookupAircraft && (normalisedAddress.Length != 6 || !normalisedAddress.All(Uri.IsHexDigit)))
            throw new ArgumentException("Enter a six-character hexadecimal aircraft address.", nameof(address));
        if (lookupAircraft && !GetServices(ApiEndpointType.Aircraft).Contains(aircraftService))
            throw new ArgumentException($"{aircraftService} is not configured for aircraft lookups.", nameof(aircraftService));
        if (lookupFlight && !GetServices(ApiEndpointType.Flights).Contains(flightService))
            throw new ArgumentException($"{flightService} is not configured for flight lookups.", nameof(flightService));

        cancellationToken.ThrowIfCancellationRequested();
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var databaseFactory = new DatabaseManagementFactory(_logger, context, 0);

        BaseStationReader.Entities.Api.Aircraft aircraft = null;
        BaseStationReader.Entities.Api.Flight flight = null;

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

        return new ReferenceLookupResult(aircraft, flight);
    }
}

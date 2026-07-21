using BaseStationReader.Api;
using BaseStationReader.Api.Wrapper;
using BaseStationReader.BusinessLogic.Database;
using BaseStationReader.Data;
using BaseStationReader.Entities.Api;
using BaseStationReader.Entities.Config;
using BaseStationReader.Interfaces.Api;
using BaseStationReader.Interfaces.Logging;
using BaseStationReader.TrackerHub.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BaseStationReader.TrackerHub.Services;

/// <summary>
/// Executes configured schedule lookups and persists their extracted flight mappings.
/// </summary>
public sealed class AirportScheduleLookupService : IAirportScheduleLookupService
{
    private static readonly TimeSpan MaximumRange = TimeSpan.FromHours(12);
    private readonly ExternalApiSettings _settings;
    private readonly ScheduleOptions _scheduleOptions;
    private readonly IDbContextFactory<BaseStationReaderDbContext> _contextFactory;
    private readonly ITrackerLogger _logger;
    private readonly IExternalApiFactory _apiFactory = new ExternalApiFactory();

    /// <summary>
    /// Initialises an airport schedule lookup service.
    /// </summary>
    public AirportScheduleLookupService(
        ExternalApiSettings settings,
        IOptions<ScheduleOptions> scheduleOptions,
        IDbContextFactory<BaseStationReaderDbContext> contextFactory,
        ITrackerLogger logger)
    {
        _settings = settings;
        _scheduleOptions = scheduleOptions.Value;
        _contextFactory = contextFactory;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AirportWeatherOption>> GetAirportsAsync(
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Airports
            .AsNoTracking()
            .OrderBy(airport => airport.Name)
            .ThenBy(airport => airport.IATA)
            .ThenBy(airport => airport.ICAO)
            .Select(airport => new AirportWeatherOption(airport.Name, airport.IATA, airport.ICAO))
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public IReadOnlyList<ApiServiceType> GetServices()
        => _settings.ApiServices
            .Where(service => service.ApiEndpoints?.Any(endpoint =>
                endpoint.EndpointType == ApiEndpointType.Schedules) == true)
            .Select(service => service.Service)
            .Distinct()
            .OrderBy(service => service.ToString(), StringComparer.OrdinalIgnoreCase)
            .ToList();

    /// <inheritdoc />
    public (DateTime From, DateTime To) GetDefaultRange(DateTime today)
    {
        var date = today.Date;
        var from = date.Add(ParseTime(_scheduleOptions.ScheduleStartTime, TimeSpan.FromHours(9)));
        var to = date.Add(ParseTime(_scheduleOptions.ScheduleEndTime, TimeSpan.FromHours(21)));

        // Keep malformed or overly broad configuration within the same constraints as interactive changes.
        if (to <= from || to - from > MaximumRange) to = from.Add(MaximumRange);
        return (from, to);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<FlightIATACodeMapping>> LookupAsync(
        ApiServiceType serviceType,
        string iata,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default)
    {
        if (!GetServices().Contains(serviceType))
            throw new ArgumentException($"{serviceType} is not configured for schedule lookups.", nameof(serviceType));

        var normalisedIata = (iata ?? string.Empty).Trim().ToUpperInvariant();
        if (normalisedIata.Length != 3 || !normalisedIata.All(character => character is >= 'A' and <= 'Z'))
            throw new ArgumentException("Select an airport with a three-letter IATA code.", nameof(iata));
        if (to <= from || to - from > MaximumRange)
            throw new ArgumentException("The schedule range must be greater than zero and no more than 12 hours.", nameof(to));

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var databaseFactory = new DatabaseManagementFactory(_logger, context, 0, _settings.MaximumLookups);
        var wrapper = _apiFactory.GetWrapperInstance(
            TrackerHttpClient.Instance, databaseFactory, serviceType, _settings);

        var mappings = await wrapper.LookupSchedulesAsync(normalisedIata, from, to);
        return mappings?.ToList() ?? [];
    }

    /// <summary>
    /// Parses one configured time while retaining a safe fallback.
    /// </summary>
    private static TimeSpan ParseTime(string value, TimeSpan fallback)
        => TimeSpan.TryParse(value, out var parsed) && parsed >= TimeSpan.Zero && parsed < TimeSpan.FromDays(1)
            ? parsed
            : fallback;
}

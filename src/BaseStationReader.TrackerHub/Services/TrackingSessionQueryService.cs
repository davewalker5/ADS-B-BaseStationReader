#nullable enable

using BaseStationReader.Data;
using BaseStationReader.Entities.Tracking;
using BaseStationReader.TrackerHub.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BaseStationReader.TrackerHub.Services;

/// <summary>
/// Implements historical queries with short-lived, no-tracking EF Core contexts.
/// </summary>
public sealed class TrackingSessionQueryService : ITrackingSessionQueryService
{
    private const int MaximumPageSize = 100;
    private readonly IDbContextFactory<BaseStationReaderDbContext> _contextFactory;
    private readonly IFlightProfileBuilder _flightProfileBuilder;
    private readonly IFlightPathBuilder _flightPathBuilder;
    private readonly DatabaseBrowserOptions _options;

    /// <summary>
    /// Initialises a query service with a factory for short-lived database contexts.
    /// </summary>
    /// <param name="contextFactory">The Tracker Hub database-context factory.</param>
    /// <param name="flightProfileBuilder">The chart-data preparation service.</param>
    /// <param name="flightPathBuilder">The geographic path preparation service.</param>
    public TrackingSessionQueryService(
        IDbContextFactory<BaseStationReaderDbContext> contextFactory,
        IFlightProfileBuilder flightProfileBuilder,
        IFlightPathBuilder flightPathBuilder,
        IOptions<DatabaseBrowserOptions> options)
    {
        // Keep persistence querying and reusable chart preparation behind injected abstractions.
        _contextFactory = contextFactory;
        _flightProfileBuilder = flightProfileBuilder;
        _flightPathBuilder = flightPathBuilder;
        _options = options.Value;
    }

    /// <inheritdoc />
    public async Task<int?> GetLatestSessionIdAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.ObservationSessions
            .AsNoTracking()
            .OrderByDescending(session => session.StartedAtUtc)
            .ThenByDescending(session => session.Id)
            .Select(session => (int?)session.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ObservationSessionSummaryDto?> GetObservationSessionSummaryAsync(
        int sessionId,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var session = await context.ObservationSessions
            .AsNoTracking()
            .Where(item => item.Id == sessionId)
            .Select(item => new
            {
                item.Id,
                item.StartedAtUtc,
                item.ProfileName,
                item.Notes,
                item.ReceiverLatitude,
                item.ReceiverLongitude,
                item.ReceiverElevation,
                item.MinimumAltitude,
                item.MaximumAltitude,
                item.MaximumDistance,
                item.IncludedBehaviours
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (session is null) return null;

        var records = await context.TrackedAircraft
            .AsNoTracking()
            .Where(record => record.SessionId == sessionId)
            .Select(record => new
            {
                record.Id,
                record.Address,
                record.Callsign,
                record.Altitude,
                record.Distance,
                record.FirstSeen,
                record.LastSeen
            })
            .ToListAsync(cancellationToken);

        var recordIds = records.Select(record => record.Id).ToArray();
        var addresses = records.Select(record => record.Address).Distinct().ToArray();
        var positions = await context.Positions
            .AsNoTracking()
            .Where(position => recordIds.Contains(position.AircraftId))
            .Select(position => new
            {
                position.AircraftId,
                position.Altitude,
                position.Distance,
                position.Timestamp
            })
            .ToListAsync(cancellationToken);

        var identifiedAddresses = await context.Aircraft
            .AsNoTracking()
            .Where(aircraft => addresses.Contains(aircraft.Address))
            .Select(aircraft => aircraft.Address)
            .Distinct()
            .ToListAsync(cancellationToken);

        var sessionEnd = records.Select(record => (DateTime?)record.LastSeen).Max();
        var sessionStartLocal = DateTime.SpecifyKind(session.StartedAtUtc, DateTimeKind.Utc).ToLocalTime();
        var resolvedCallsigns = await context.Sightings
            .AsNoTracking()
            .Where(sighting => addresses.Contains(sighting.Aircraft.Address) &&
                               sighting.Timestamp >= sessionStartLocal &&
                               (!sessionEnd.HasValue || sighting.Timestamp <= sessionEnd.Value))
            .Select(sighting => new
            {
                sighting.Aircraft.Address,
                sighting.Timestamp,
                Callsign = sighting.Flight.Callsign
            })
            .ToListAsync(cancellationToken);

        var callsigns = records
            .Select(record => record.Callsign?.Trim())
            .Where(callsign => !string.IsNullOrWhiteSpace(callsign))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var resolvedFlights = callsigns.Count(callsign => resolvedCallsigns.Any(sighting =>
            callsign!.Equals(sighting.Callsign?.Trim(), StringComparison.OrdinalIgnoreCase) &&
            records.Any(record => record.Address == sighting.Address &&
                                  sighting.Timestamp >= record.FirstSeen &&
                                  sighting.Timestamp <= record.LastSeen)));

        var positionRecordIds = positions.Select(position => position.AircraftId).Distinct().ToHashSet();
        var altitudeCandidates = positions
            .Where(position => position.Altitude.HasValue)
            .Select(position => (AircraftId: position.AircraftId, Altitude: position.Altitude))
            .Concat(records.Where(record => record.Altitude.HasValue)
                .Select(record => (AircraftId: record.Id, Altitude: record.Altitude)));
        var distanceCandidates = positions
            .Where(position => position.Distance.HasValue)
            .Select(position => (AircraftId: position.AircraftId, Distance: position.Distance))
            .Concat(records.Where(record => record.Distance.HasValue)
                .Select(record => (AircraftId: record.Id, Distance: record.Distance)));

        var lowest = altitudeCandidates.OrderBy(item => item.Altitude).FirstOrDefault();
        var highest = altitudeCandidates.OrderByDescending(item => item.Altitude).FirstOrDefault();
        var furthest = distanceCandidates.OrderByDescending(item => item.Distance).FirstOrDefault();
        var longest = records.OrderByDescending(record => record.LastSeen - record.FirstSeen).FirstOrDefault();
        var lastActivity = records.Select(record => (DateTime?)record.LastSeen)
            .Concat(positions.Select(position => (DateTime?)position.Timestamp))
            .Max();
        var distinctAircraft = addresses.Length;
        var identifiedAircraft = identifiedAddresses.Count;

        ObservationHighlightDto? Highlight(int aircraftId, decimal? altitude = null,
            double? distance = null, TimeSpan? duration = null)
        {
            if (aircraftId == 0) return null;
            var record = records.First(item => item.Id == aircraftId);
            return new ObservationHighlightDto
            {
                Address = record.Address,
                Callsign = record.Callsign?.Trim() ?? string.Empty,
                Altitude = altitude,
                Distance = distance,
                Duration = duration
            };
        }

        return new ObservationSessionSummaryDto
        {
            SessionId = session.Id,
            StartedAtUtc = session.StartedAtUtc,
            LastActivity = lastActivity,
            ObservedDuration = lastActivity.HasValue
                ? lastActivity.Value - sessionStartLocal
                : TimeSpan.Zero,
            ProfileName = session.ProfileName,
            Notes = session.Notes ?? string.Empty,
            ReceiverLatitude = session.ReceiverLatitude,
            ReceiverLongitude = session.ReceiverLongitude,
            ReceiverElevation = session.ReceiverElevation,
            MinimumAltitudeLimit = session.MinimumAltitude,
            MaximumAltitudeLimit = session.MaximumAltitude,
            MaximumDistanceLimit = session.MaximumDistance,
            IncludedBehaviours = session.IncludedBehaviours,
            ObservationRecords = records.Count,
            DistinctAircraft = distinctAircraft,
            DistinctCallsigns = callsigns.Length,
            AircraftWithPositionHistory = positionRecordIds.Count,
            PositionRecords = positions.Count,
            IdentifiedAircraft = identifiedAircraft,
            ResolvedFlights = resolvedFlights,
            UnidentifiedAircraft = distinctAircraft - identifiedAircraft,
            AircraftResolutionPercentage = Percentage(identifiedAircraft, distinctAircraft),
            FlightResolutionPercentage = Percentage(resolvedFlights, callsigns.Length),
            LowestAltitude = Highlight(lowest.AircraftId, altitude: lowest.Altitude),
            HighestAltitude = Highlight(highest.AircraftId, altitude: highest.Altitude),
            FurthestAircraft = Highlight(furthest.AircraftId, distance: furthest.Distance),
            LongestObservedAircraft = longest is null
                ? null
                : Highlight(longest.Id, duration: longest.LastSeen - longest.FirstSeen)
        };
    }

    private static double Percentage(int resolved, int total)
        => total == 0 ? 0 : Math.Round(resolved * 100d / total, 1);

    /// <inheritdoc />
    public async Task<IReadOnlyList<ObservationSessionOptionDto>> ListSessionsAsync(
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var historyDays = _options.SessionHistoryDays > 0 ? _options.SessionHistoryDays : 7;
        var earliestSession = DateTime.UtcNow.AddDays(-historyDays);

        return await context.ObservationSessions
            .AsNoTracking()
            .Where(session => session.StartedAtUtc >= earliestSession)
            .OrderByDescending(session => session.StartedAtUtc)
            .ThenByDescending(session => session.Id)
            .Select(session => new ObservationSessionOptionDto
            {
                Id = session.Id,
                ProfileName = session.ProfileName,
                StartedAtUtc = session.StartedAtUtc
            })
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<PagedResult<ObservationSessionDto>> SearchObservationSessionsAsync(
        ObservationSessionFilter filter,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filter);

        var page = Math.Max(1, filter.Page);
        var pageSize = Math.Clamp(filter.PageSize, 1, MaximumPageSize);

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var query = context.ObservationSessions.AsNoTracking().AsQueryable();

        if (filter.SessionId.HasValue)
        {
            query = query.Where(session => session.Id == filter.SessionId.Value);
        }

        // Session date filters deliberately apply to the persisted UTC start timestamp.
        if (filter.FromDate.HasValue)
        {
            query = query.Where(session => session.StartedAtUtc >= filter.FromDate.Value.Date.ToUniversalTime());
        }

        if (filter.ToDate.HasValue)
        {
            var exclusiveEnd = filter.ToDate.Value.Date.AddDays(1).ToUniversalTime();
            query = query.Where(session => session.StartedAtUtc < exclusiveEnd);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(session => session.StartedAtUtc)
            .ThenByDescending(session => session.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(session => new ObservationSessionDto
            {
                SessionId = session.Id,
                StartedAtUtc = session.StartedAtUtc,
                ProfileName = session.ProfileName,
                Notes = session.Notes,
                Host = session.Host,
                Port = session.Port,
                ReceiverLatitude = session.ReceiverLatitude,
                ReceiverLongitude = session.ReceiverLongitude,
                ReceiverElevation = session.ReceiverElevation,
                MinimumAltitude = session.MinimumAltitude,
                MaximumAltitude = session.MaximumAltitude,
                MaximumDistance = session.MaximumDistance,
                IncludedBehaviours = session.IncludedBehaviours
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<ObservationSessionDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    /// <inheritdoc />
    public async Task<PagedResult<TrackingSessionSummaryDto>> SearchAsync(
        TrackingSessionFilter filter,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filter);

        // Normalise paging and text input so every query remains bounded and predictable.
        var page = Math.Max(1, filter.Page);
        var pageSize = Math.Clamp(filter.PageSize, 1, MaximumPageSize);
        var address = filter.Address.Trim();
        var callsign = filter.Callsign.Trim();
        var registration = filter.Registration.Trim();
        var airline = filter.Airline.Trim();
        var flightNumber = filter.FlightNumber.Trim();

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var query = context.TrackedAircraft.AsNoTracking().AsQueryable();

        // Apply scalar tracking-record filters first so indexed columns can reduce the candidate set.
        if (filter.SessionId.HasValue)
        {
            query = query.Where(record => record.SessionId == filter.SessionId.Value);
        }

        if (filter.FromDate.HasValue)
        {
            query = query.Where(record => record.LastSeen >= filter.FromDate.Value.Date);
        }

        if (filter.ToDate.HasValue)
        {
            var exclusiveEnd = filter.ToDate.Value.Date.AddDays(1);
            query = query.Where(record => record.FirstSeen < exclusiveEnd);
        }

        if (!string.IsNullOrWhiteSpace(address))
        {
            query = query.Where(record => record.Address.StartsWith(address));
        }

        if (!string.IsNullOrWhiteSpace(callsign))
        {
            query = query.Where(record => record.Callsign != null && record.Callsign.Contains(callsign));
        }

        if (filter.MinimumAltitude.HasValue)
        {
            query = query.Where(record => record.Altitude >= filter.MinimumAltitude.Value);
        }

        if (filter.MaximumAltitude.HasValue)
        {
            query = query.Where(record => record.Altitude <= filter.MaximumAltitude.Value);
        }

        if (filter.MinimumDistance.HasValue)
        {
            query = query.Where(record => record.Distance >= filter.MinimumDistance.Value);
        }

        if (filter.MaximumDistance.HasValue)
        {
            query = query.Where(record => record.Distance <= filter.MaximumDistance.Value);
        }

        if (filter.Status.HasValue)
        {
            query = query.Where(record => record.Status == filter.Status.Value);
        }

        if (filter.CompletedOnly)
        {
            query = query.Where(record => record.Status == TrackingStatus.Locked);
        }

        if (filter.HasPositions.HasValue)
        {
            query = filter.HasPositions.Value
                ? query.Where(record => context.Positions.Any(position => position.AircraftId == record.Id))
                : query.Where(record => !context.Positions.Any(position => position.AircraftId == record.Id));
        }

        // Resolve aircraft metadata by ICAO address without exposing persistence entities to callers.
        if (!string.IsNullOrWhiteSpace(registration))
        {
            query = query.Where(record => context.Aircraft.Any(aircraft =>
                aircraft.Address == record.Address && aircraft.Registration.Contains(registration)));
        }

        // Sightings link aircraft and flights by observation time, so constrain matches to the session window.
        if (!string.IsNullOrWhiteSpace(airline))
        {
            query = query.Where(record => context.Sightings.Any(sighting =>
                sighting.Aircraft.Address == record.Address &&
                sighting.Timestamp >= record.FirstSeen &&
                sighting.Timestamp <= record.LastSeen &&
                sighting.Flight.Airline != null &&
                (sighting.Flight.Airline.Name.Contains(airline) ||
                 sighting.Flight.Airline.IATA.Contains(airline) ||
                 sighting.Flight.Airline.ICAO.Contains(airline))));
        }

        if (!string.IsNullOrWhiteSpace(flightNumber))
        {
            query = query.Where(record => context.Sightings.Any(sighting =>
                sighting.Aircraft.Address == record.Address &&
                sighting.Timestamp >= record.FirstSeen &&
                sighting.Timestamp <= record.LastSeen &&
                (sighting.Flight.IATA.Contains(flightNumber) || sighting.Flight.ICAO.Contains(flightNumber))));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        // Page the base records before loading aggregates to avoid scanning position rows for discarded results.
        var records = await query
            .OrderByDescending(record => record.LastSeen)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(record => new
            {
                record.Id,
                record.Address,
                record.Callsign,
                record.FirstSeen,
                record.LastSeen,
                record.Altitude,
                record.Distance,
                record.Status
            })
            .ToListAsync(cancellationToken);

        var recordIds = records.Select(record => record.Id).ToArray();
        var addresses = records.Select(record => record.Address).Distinct().ToArray();

        // Load only aggregates for the current page; full position histories remain deferred to later releases.
        var positionRows = await context.Positions
            .AsNoTracking()
            .Where(position => recordIds.Contains(position.AircraftId))
            .Select(position => new
            {
                position.AircraftId,
                position.Timestamp,
                position.Altitude,
                position.Distance
            })
            .ToListAsync(cancellationToken);

        var aircraftRows = await context.Aircraft
            .AsNoTracking()
            .Where(aircraft => addresses.Contains(aircraft.Address))
            .Select(aircraft => new { aircraft.Address, aircraft.Registration })
            .ToListAsync(cancellationToken);

        // Tolerate legacy duplicate lookup rows by selecting one registration for each ICAO address.
        var registrations = aircraftRows
            .GroupBy(aircraft => aircraft.Address, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First().Registration, StringComparer.OrdinalIgnoreCase);

        var items = records.Select(record =>
        {
            // Aggregate in memory over the bounded current-page position set to avoid SQLite decimal limitations.
            var positions = positionRows
                .Where(position => position.AircraftId == record.Id)
                .OrderBy(position => position.Timestamp)
                .ToArray();

            return new TrackingSessionSummaryDto
            {
                Id = record.Id,
                Address = record.Address,
                Callsign = record.Callsign ?? string.Empty,
                Registration = registrations.GetValueOrDefault(record.Address, string.Empty),
                FirstSeen = record.FirstSeen,
                LastSeen = record.LastSeen,
                InitialAltitude = positions.FirstOrDefault()?.Altitude ?? record.Altitude,
                FinalAltitude = positions.LastOrDefault()?.Altitude ?? record.Altitude,
                MinimumAltitude = positions.Where(position => position.Altitude.HasValue).Select(position => position.Altitude).Min() ?? record.Altitude,
                MaximumAltitude = positions.Where(position => position.Altitude.HasValue).Select(position => position.Altitude).Max() ?? record.Altitude,
                MinimumDistance = positions.Where(position => position.Distance.HasValue).Select(position => position.Distance).Min() ?? record.Distance,
                MaximumDistance = positions.Where(position => position.Distance.HasValue).Select(position => position.Distance).Max() ?? record.Distance,
                PositionCount = positions.Length,
                Status = record.Status
            };
        }).ToArray();

        return new PagedResult<TrackingSessionSummaryDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    /// <inheritdoc />
    public async Task<TrackingSessionDetailDto?> GetAsync(
        int trackingRecordId,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        // Project the record before loading related information so no tracked entity leaves this service.
        var record = await context.TrackedAircraft
            .AsNoTracking()
            .Where(aircraft => aircraft.Id == trackingRecordId)
            .Select(aircraft => new
            {
                aircraft.Id,
                aircraft.Address,
                aircraft.Callsign,
                aircraft.Squawk,
                aircraft.Altitude,
                aircraft.GroundSpeed,
                aircraft.Track,
                aircraft.Latitude,
                aircraft.Longitude,
                aircraft.Distance,
                aircraft.VerticalRate,
                aircraft.FirstSeen,
                aircraft.LastSeen,
                aircraft.Messages,
                aircraft.Status
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (record is null)
        {
            return null;
        }

        var positions = await context.Positions
            .AsNoTracking()
            .Where(position => position.AircraftId == trackingRecordId)
            .OrderBy(position => position.Timestamp)
            .Select(position => new PositionSummaryDto
            {
                Timestamp = position.Timestamp,
                Latitude = position.Latitude,
                Longitude = position.Longitude,
                Altitude = position.Altitude,
                Distance = position.Distance
            })
            .ToListAsync(cancellationToken);

        var aircraftInfo = await context.Aircraft
            .AsNoTracking()
            .Where(aircraft => aircraft.Address == record.Address)
            .Select(aircraft => new
            {
                aircraft.Registration,
                aircraft.Manufactured,
                aircraft.Age,
                ModelName = aircraft.Model.Name,
                ModelIcao = aircraft.Model.ICAO,
                Manufacturer = aircraft.Model.Manufacturer.Name
            })
            .FirstOrDefaultAsync(cancellationToken);

        // Select the closest sighting inside the session as the best available flight association.
        var flightInfo = await context.Sightings
            .AsNoTracking()
            .Where(sighting =>
                sighting.Aircraft.Address == record.Address &&
                sighting.Timestamp >= record.FirstSeen &&
                sighting.Timestamp <= record.LastSeen)
            .OrderBy(sighting => sighting.Timestamp)
            .Select(sighting => new
            {
                FlightIata = sighting.Flight.IATA,
                FlightIcao = sighting.Flight.ICAO,
                sighting.Flight.Embarkation,
                sighting.Flight.Destination,
                AirlineName = sighting.Flight.Airline == null ? string.Empty : sighting.Flight.Airline.Name
            })
            .FirstOrDefaultAsync(cancellationToken);

        return new TrackingSessionDetailDto
        {
            Id = record.Id,
            Address = record.Address,
            Callsign = record.Callsign ?? string.Empty,
            Squawk = record.Squawk ?? string.Empty,
            Altitude = record.Altitude,
            GroundSpeed = record.GroundSpeed,
            Track = record.Track,
            Latitude = record.Latitude,
            Longitude = record.Longitude,
            Distance = record.Distance,
            VerticalRate = record.VerticalRate,
            FirstSeen = record.FirstSeen,
            LastSeen = record.LastSeen,
            Messages = record.Messages,
            Status = record.Status,
            PositionCount = positions.Count,
            MinimumAltitude = positions.Where(position => position.Altitude.HasValue).Select(position => position.Altitude).Min() ?? record.Altitude,
            MaximumAltitude = positions.Where(position => position.Altitude.HasValue).Select(position => position.Altitude).Max() ?? record.Altitude,
            MinimumDistance = positions.Where(position => position.Distance.HasValue).Select(position => position.Distance).Min() ?? record.Distance,
            MaximumDistance = positions.Where(position => position.Distance.HasValue).Select(position => position.Distance).Max() ?? record.Distance,
            FirstPosition = positions.FirstOrDefault(),
            FinalPosition = positions.LastOrDefault(),
            Registration = aircraftInfo?.Registration ?? string.Empty,
            Manufactured = aircraftInfo?.Manufactured,
            AircraftAge = aircraftInfo?.Age,
            ModelName = aircraftInfo?.ModelName ?? string.Empty,
            ModelIcao = aircraftInfo?.ModelIcao ?? string.Empty,
            Manufacturer = aircraftInfo?.Manufacturer ?? string.Empty,
            FlightIata = flightInfo?.FlightIata ?? string.Empty,
            FlightIcao = flightInfo?.FlightIcao ?? string.Empty,
            Embarkation = flightInfo?.Embarkation ?? string.Empty,
            Destination = flightInfo?.Destination ?? string.Empty,
            AirlineName = flightInfo?.AirlineName ?? string.Empty
        };
    }

    /// <inheritdoc />
    public async Task<FlightProfileDto?> GetFlightProfileAsync(
        int trackingRecordId,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        // Load only identity fields before requesting the selected record's position history.
        var record = await context.TrackedAircraft
            .AsNoTracking()
            .Where(aircraft => aircraft.Id == trackingRecordId)
            .Select(aircraft => new { aircraft.Id, aircraft.Address, aircraft.Callsign })
            .SingleOrDefaultAsync(cancellationToken);

        if (record is null)
        {
            return null;
        }

        var points = await context.Positions
            .AsNoTracking()
            .Where(position => position.AircraftId == trackingRecordId)
            .OrderBy(position => position.Timestamp)
            .ThenBy(position => position.Id)
            .Select(position => new FlightProfilePointDto
            {
                Timestamp = position.Timestamp,
                Latitude = position.Latitude,
                Longitude = position.Longitude,
                Altitude = position.Altitude,
                Distance = position.Distance
            })
            .ToListAsync(cancellationToken);

        // Delegate ordering, enrichment, and summaries to independently testable preparation logic.
        return _flightProfileBuilder.Build(record.Id, record.Address, record.Callsign ?? string.Empty, points);
    }

    /// <inheritdoc />
    public async Task<FlightPathDto?> GetFlightPathAsync(
        int trackingRecordId,
        CancellationToken cancellationToken = default)
    {
        // Reuse the detail projection for consistent aircraft and flight metadata.
        var detail = await GetAsync(trackingRecordId, cancellationToken);
        if (detail is null)
        {
            return null;
        }

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var points = await context.Positions
            .AsNoTracking()
            .Where(position => position.AircraftId == trackingRecordId)
            .OrderBy(position => position.Timestamp)
            .ThenBy(position => position.Id)
            .Select(position => new FlightProfilePointDto
            {
                Timestamp = position.Timestamp,
                Latitude = position.Latitude,
                Longitude = position.Longitude,
                Altitude = position.Altitude,
                Distance = position.Distance
            })
            .ToListAsync(cancellationToken);

        var flightNumber = !string.IsNullOrWhiteSpace(detail.FlightIata) ? detail.FlightIata : detail.FlightIcao;
        var route = string.IsNullOrWhiteSpace(detail.Embarkation) && string.IsNullOrWhiteSpace(detail.Destination)
            ? string.Empty
            : $"{detail.Embarkation} → {detail.Destination}";

        // Delegate all notebook-derived transformations to independently testable C# logic.
        return _flightPathBuilder.Build(
            detail.Id,
            detail.Address,
            detail.Callsign,
            detail.Registration,
            detail.ModelName,
            flightNumber,
            detail.AirlineName,
            route,
            points);
    }
}

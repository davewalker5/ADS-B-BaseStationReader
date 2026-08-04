#nullable enable

using BaseStationReader.Data;
using BaseStationReader.BusinessLogic.Database;
using BaseStationReader.Entities.Hub;
using BaseStationReader.Entities.Tracking;
using BaseStationReader.Interfaces.Logging;
using BaseStationReader.Interfaces.Tracking;
using Microsoft.EntityFrameworkCore;

namespace BaseStationReader.BusinessLogic.Tracking;

/// <summary>
/// Combines live aircraft, persisted session observations, local references, and transient cache state.
/// </summary>
public sealed class LiveTrackerStatusService(
    IDbContextFactory<BaseStationReaderDbContext> contextFactory,
    ITransientReferenceStatusProvider transientStatus,
    ILiveTrackerStatisticsProvider statistics,
    ITrackerLogger logger) : ILiveTrackerStatusService
{
    /// <inheritdoc />
    public async Task<LiveTrackerStatusDto?> GetAsync(
        int? sessionId,
        IReadOnlyCollection<TrackedAircraftDto> aircraft,
        bool isRunning,
        CancellationToken cancellationToken = default)
    {
        // A materialised snapshot keeps every calculation internally consistent while live updates continue.
        var liveAircraft = aircraft.ToArray();
        if (!sessionId.HasValue)
        {
            return null;
        }

        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var databaseFactory = new DatabaseManagementFactory(logger, context, 0);

        // Resolve session metadata through business logic before combining it with live in-memory state.
        var session = await databaseFactory.ObservationSessionManager
            .GetAsync(sessionId.Value, cancellationToken);
        if (session is null)
        {
            return null;
        }

        // Normalise current identities to match the uppercase reference-data keys used by imports.
        var addresses = liveAircraft.Select(item => item.Address.Trim().ToUpperInvariant()).Distinct().ToArray();
        var callsigns = liveAircraft
            .Select(item => item.Callsign?.Trim().ToUpperInvariant())
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Cast<string>()
            .Distinct()
            .ToArray();
        // Route local reference reads through the same business-logic managers used by the editor UI.
        var localAircraft = await databaseFactory.AircraftManager
            .ListAsync(item => addresses.Contains(item.Address));
        cancellationToken.ThrowIfCancellationRequested();
        var localFlights = await databaseFactory.FlightManager
            .ListAsync(item => callsigns.Contains(item.Callsign));
        cancellationToken.ThrowIfCancellationRequested();

        // Count distinct normalized identities in case imported reference data contains duplicate rows.
        var localAddresses = localAircraft
            .Select(item => item.Address)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();
        var localCallsigns = localFlights
            .Select(item => item.Callsign)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();
        // Read process-memory resolution counts through a narrow contract without depending on cache implementation.
        var transient = transientStatus.GetReferenceLookupStatus();

        // Combine persisted session identity, live statistics, and resolution counts into one reusable snapshot.
        return new LiveTrackerStatusDto
        {
            IsRunning = isRunning,
            StartedAtUtc = session.StartedAtUtc,
            ProfileName = session.ProfileName,
            Notes = session.Notes,
            CurrentlyTracked = liveAircraft.Length,
            AircraftAdded = statistics.AircraftAdded,
            AircraftRemoved = statistics.AircraftRemoved,
            PositionRecords = statistics.PositionRecordsWritten,
            MessagesProcessed = statistics.MessagesProcessed,
            DistinctAircraft = statistics.DistinctAircraft,
            DistinctCallsigns = statistics.DistinctCallsigns,
            AircraftWithPositionRecords = statistics.AircraftWithPositionRecords,
            AircraftLocallyResolved = localAddresses,
            AircraftUnresolved = Math.Max(0, addresses.Length - localAddresses),
            FlightsLocallyResolved = localCallsigns,
            FlightsUnresolved = Math.Max(0, callsigns.Length - localCallsigns),
            AircraftWithoutCallsign = liveAircraft.Count(item => string.IsNullOrWhiteSpace(item.Callsign)),
            AircraftTransientlyResolved = transient.AircraftResolved,
            FlightsTransientlyResolved = transient.FlightsResolved
        };
    }
}

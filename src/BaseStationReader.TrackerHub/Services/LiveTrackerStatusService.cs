#nullable enable

using BaseStationReader.Data;
using BaseStationReader.Entities.Hub;
using BaseStationReader.TrackerHub.Models;
using Microsoft.EntityFrameworkCore;

namespace BaseStationReader.TrackerHub.Services;

/// <summary>
/// Combines live aircraft, persisted session observations, local references, and transient cache state.
/// </summary>
public sealed class LiveTrackerStatusService(
    IDbContextFactory<BaseStationReaderDbContext> contextFactory,
    ITransientResponseCache cache,
    TrackingRuntime runtime) : ILiveTrackerStatusService
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
        if (!sessionId.HasValue) return null;

        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var session = await context.ObservationSessions
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == sessionId.Value, cancellationToken);
        if (session is null) return null;

        // Normalise current identities to match the uppercase reference-data keys used by imports.
        var addresses = liveAircraft.Select(item => item.Address.Trim().ToUpperInvariant()).Distinct().ToArray();
        var callsigns = liveAircraft
            .Select(item => item.Callsign?.Trim().ToUpperInvariant())
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Cast<string>()
            .Distinct()
            .ToArray();
        var localAddresses = await context.Aircraft.AsNoTracking()
            .Where(item => addresses.Contains(item.Address))
            .Select(item => item.Address)
            .Distinct()
            .CountAsync(cancellationToken);
        var localCallsigns = await context.Flights.AsNoTracking()
            .Where(item => callsigns.Contains(item.Callsign))
            .Select(item => item.Callsign)
            .Distinct()
            .CountAsync(cancellationToken);
        var transient = cache.GetReferenceLookupStatus();

        return new LiveTrackerStatusDto
        {
            IsRunning = isRunning,
            StartedAtUtc = session.StartedAtUtc,
            ProfileName = session.ProfileName,
            Notes = session.Notes,
            CurrentlyTracked = liveAircraft.Length,
            AircraftAdded = runtime.AircraftAdded,
            AircraftRemoved = runtime.AircraftRemoved,
            PositionRecords = runtime.PositionRecordsWritten,
            MessagesProcessed = runtime.MessagesProcessed,
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

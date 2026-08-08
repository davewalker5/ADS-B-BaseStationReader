using BaseStationReader.Entities.Tracking;
using BaseStationReader.Entities.Logging;
using BaseStationReader.Interfaces.Database;
using BaseStationReader.Interfaces.Logging;
using BaseStationReader.Interfaces.Tracking;

namespace BaseStationReader.BusinessLogic.Database;

/// <summary>
/// Resolves aircraft observations to deterministic, session-scoped tracking lifetimes.
/// </summary>
internal sealed class AircraftLifetimeManager : IAircraftLifetimeManager
{
    private readonly ITrackedAircraftWriter _writer;
    private readonly ITrackerLogger _logger;
    private readonly int _timeToLock;

    /// <summary>
    /// Initialises a tracking-lifetime manager.
    /// </summary>
    /// <param name="writer">Tracked-aircraft persistence manager.</param>
    /// <param name="logger">Tracker logger.</param>
    /// <param name="timeToLockMs">Observation gap that starts a new lifetime, in milliseconds.</param>
    public AircraftLifetimeManager(
        ITrackedAircraftWriter writer,
        ITrackerLogger logger,
        int timeToLockMs)
    {
        _writer = writer;
        _logger = logger;
        _timeToLock = timeToLockMs;
    }

    /// <inheritdoc />
    public async Task<TrackedAircraft> ResolveAsync(
        string address,
        int sessionId,
        DateTime observedAt,
        CancellationToken cancellationToken = default)
    {
        var aircraft = await GetCurrentAsync(address, sessionId, cancellationToken).ConfigureAwait(false);
        if (aircraft == null || aircraft.Status == TrackingStatus.Locked)
        {
            return null;
        }

        // Observation time makes replay deterministic: queue age and processing time cannot split a lifetime.
        var observationGap = observedAt - aircraft.LastSeen;
        if (observationGap.TotalMilliseconds < _timeToLock)
        {
            return aircraft;
        }

        aircraft.Status = TrackingStatus.Locked;
        await _writer.WriteAsync(aircraft, cancellationToken).ConfigureAwait(false);
        _logger.LogMessage(
            Severity.Debug,
            $"Locked aircraft lifetime: address={address}, session={sessionId}, " +
            $"previousLastSeen={aircraft.LastSeen:O}, incomingObservedAt={observedAt:O}, " +
            $"gapMs={observationGap.TotalMilliseconds:F0}");

        return null;
    }

    /// <inheritdoc />
    public async Task<TrackedAircraft> GetCurrentAsync(
        string address,
        int sessionId,
        CancellationToken cancellationToken = default)
        => await _writer.GetAsync(
            aircraft => aircraft.Address == address && aircraft.SessionId == sessionId,
            cancellationToken).ConfigureAwait(false);
}

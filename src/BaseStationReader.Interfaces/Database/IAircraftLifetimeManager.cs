using BaseStationReader.Entities.Tracking;

namespace BaseStationReader.Interfaces.Database;

/// <summary>
/// Resolves persisted aircraft observations to session-scoped tracking lifetimes.
/// </summary>
public interface IAircraftLifetimeManager
{
    /// <summary>
    /// Resolves an aircraft observation to its existing lifetime or locks an expired lifetime.
    /// </summary>
    /// <param name="address">ICAO aircraft address.</param>
    /// <param name="sessionId">Observation session identifier.</param>
    /// <param name="observedAt">Timestamp carried by the observation.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The reusable lifetime, or <see langword="null"/> when a new lifetime is required.</returns>
    Task<TrackedAircraft> ResolveAsync(
        string address,
        int sessionId,
        DateTime observedAt,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the most recent lifetime for an aircraft in an observation session without changing its state.
    /// </summary>
    /// <param name="address">ICAO aircraft address.</param>
    /// <param name="sessionId">Observation session identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The most recent lifetime, or <see langword="null"/> when none exists.</returns>
    Task<TrackedAircraft> GetCurrentAsync(
        string address,
        int sessionId,
        CancellationToken cancellationToken = default);
}

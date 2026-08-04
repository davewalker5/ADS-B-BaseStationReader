using BaseStationReader.Interfaces.Tracking;

namespace BaseStationReader.TrackerHub.Services;

/// <summary>
/// Caches transient lookup responses in the current process only.
/// Implementations must never serialize or persist request or response data to a database or file.
/// </summary>
public interface ITransientResponseCache : ITransientReferenceStatusProvider
{
    /// <summary>
    /// Returns an in-memory response or creates and temporarily retains one.
    /// </summary>
    /// <typeparam name="T">The response type held as an in-process object reference.</typeparam>
    /// <param name="key">A normalized, non-secret request identity.</param>
    /// <param name="lifetime">How long the object may remain in process memory.</param>
    /// <param name="factory">Creates the response when no live entry exists.</param>
    /// <param name="cancellationToken">Cancels waiting or response creation.</param>
    /// <returns>The cached or newly created response.</returns>
    Task<T> GetOrCreateAsync<T>(
        string key,
        TimeSpan lifetime,
        Func<CancellationToken, Task<T>> factory,
        CancellationToken cancellationToken = default);
}

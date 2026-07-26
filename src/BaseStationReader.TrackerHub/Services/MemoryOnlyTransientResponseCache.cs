#nullable enable

using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;

namespace BaseStationReader.TrackerHub.Services;

/// <summary>
/// Stores bounded transient responses solely as object references in process memory.
/// This implementation deliberately uses <see cref="MemoryCache"/> directly: it has no serializer,
/// database provider, distributed backing store, file path, or background disk persistence.
/// All cached request and response data is lost when entries expire or TrackerHub exits.
/// </summary>
public sealed class MemoryOnlyTransientResponseCache : ITransientResponseCache, IDisposable
{
    private const int MaximumEntries = 512;
    private readonly MemoryCache _cache = new(new MemoryCacheOptions
    {
        // A bounded entry count prevents transient API data from growing without limit.
        SizeLimit = MaximumEntries
    });
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _entryLocks =
        new(StringComparer.Ordinal);

    /// <inheritdoc />
    public async Task<T> GetOrCreateAsync<T>(
        string key,
        TimeSpan lifetime,
        Func<CancellationToken, Task<T>> factory,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(factory);
        if (lifetime <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(lifetime), "Cache lifetime must be greater than zero.");

        if (_cache.TryGetValue(key, out T? cached) && cached is not null)
        {
            return cached;
        }

        var entryLock = _entryLocks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
        await entryLock.WaitAsync(cancellationToken);
        try
        {
            // Recheck after entering the per-key gate so simultaneous identical requests make one API call.
            if (_cache.TryGetValue(key, out cached) && cached is not null)
            {
                return cached;
            }

            // Only a successfully completed factory result is retained; exceptions and cancellations are never cached.
            var created = await factory(cancellationToken);
            _cache.Set(key, created, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = lifetime,
                Size = 1
            });
            return created;
        }
        finally
        {
            entryLock.Release();
            _entryLocks.TryRemove(new KeyValuePair<string, SemaphoreSlim>(key, entryLock));
        }
    }

    /// <summary>
    /// Releases the process-local cache and its in-memory entries.
    /// No cache content is written anywhere during disposal.
    /// </summary>
    public void Dispose()
    {
        _cache.Dispose();
        foreach (var entryLock in _entryLocks.Values)
        {
            entryLock.Dispose();
        }
        _entryLocks.Clear();
    }
}

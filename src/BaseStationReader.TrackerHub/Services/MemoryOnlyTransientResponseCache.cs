#nullable enable

using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;
using BaseStationReader.TrackerHub.Models;

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
    private readonly ConcurrentDictionary<string, byte> _knownKeys = new(StringComparer.Ordinal);

    /// <inheritdoc />
    public TransientLookupStatus GetReferenceLookupStatus()
    {
        // Read only live reference entries so expired cache responses never inflate the status counts.
        var results = new List<ReferenceLookupResult>();
        foreach (var key in _knownKeys.Keys)
        {
            if (_cache.TryGetValue(key, out ReferenceLookupResult? result) && result is not null)
            {
                results.Add(result);
            }
            else
            {
                // Discard metadata for expired and non-reference entries without retaining their response values.
                _knownKeys.TryRemove(key, out _);
            }
        }

        // Count distinct identities because one combined request can be cached under several provider choices.
        var aircraft = results
            .Where(result => result.AircraftSource == ReferenceLookupSource.Api && result.Aircraft is not null)
            .Select(result => result.Aircraft!.Address)
            .Where(address => !string.IsNullOrWhiteSpace(address))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();
        var flights = results
            .Where(result => result.FlightSource == ReferenceLookupSource.Api && result.Flight is not null)
            .Select(result => result.Flight!.Callsign)
            .Where(callsign => !string.IsNullOrWhiteSpace(callsign))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();
        return new TransientLookupStatus(aircraft, flights);
    }

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
            // Retain only cache keys, never a second copy of transient response data.
            _knownKeys.TryAdd(key, 0);
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
        _knownKeys.Clear();
    }
}

using BaseStationReader.TrackerHub.Services;

namespace BaseStationReader.Tests.TrackerHub;

[TestClass]
public class MemoryOnlyTransientResponseCacheTest
{
    /// <summary>
    /// Verifies that repeated identical requests reuse the in-process response object.
    /// </summary>
    [TestMethod]
    public async Task ReuseCachedResponseTestAsync()
    {
        using var cache = new MemoryOnlyTransientResponseCache();
        var calls = 0;

        var first = await cache.GetOrCreateAsync(
            "weather:METAR:Test:EGLL",
            TimeSpan.FromMinutes(5),
            _ => Task.FromResult(++calls));
        var second = await cache.GetOrCreateAsync(
            "weather:METAR:Test:EGLL",
            TimeSpan.FromMinutes(5),
            _ => Task.FromResult(++calls));

        Assert.AreEqual(1, first);
        Assert.AreEqual(1, second);
        Assert.AreEqual(1, calls);
    }

    /// <summary>
    /// Verifies that simultaneous requests share one in-flight response creation.
    /// </summary>
    [TestMethod]
    public async Task CoalesceConcurrentRequestsTestAsync()
    {
        using var cache = new MemoryOnlyTransientResponseCache();
        var calls = 0;
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        async Task<string> CreateAsync(CancellationToken _)
        {
            Interlocked.Increment(ref calls);
            await release.Task;
            return "response";
        }

        var first = cache.GetOrCreateAsync(
            "schedule:Test:LHR:1:2", TimeSpan.FromMinutes(15), CreateAsync);
        var second = cache.GetOrCreateAsync(
            "schedule:Test:LHR:1:2", TimeSpan.FromMinutes(15), CreateAsync);
        release.SetResult();

        var responses = await Task.WhenAll(first, second);

        CollectionAssert.AreEqual(new[] { "response", "response" }, responses);
        Assert.AreEqual(1, calls);
    }

    /// <summary>
    /// Verifies that failed operations are not retained as cache entries.
    /// </summary>
    [TestMethod]
    public async Task DoNotCacheFailureTestAsync()
    {
        using var cache = new MemoryOnlyTransientResponseCache();
        var calls = 0;

        await Assert.ThrowsAsync<InvalidOperationException>(() => cache.GetOrCreateAsync<int>(
            "reference:failure",
            TimeSpan.FromHours(1),
            _ =>
            {
                calls++;
                throw new InvalidOperationException("API unavailable");
            }));
        var response = await cache.GetOrCreateAsync(
            "reference:failure",
            TimeSpan.FromHours(1),
            _ => Task.FromResult(++calls));

        Assert.AreEqual(2, response);
        Assert.AreEqual(2, calls);
    }

    /// <summary>
    /// Verifies that separate cache instances share no data, consistent with process-memory-only storage.
    /// </summary>
    [TestMethod]
    public async Task KeepInstancesIsolatedTestAsync()
    {
        using var firstCache = new MemoryOnlyTransientResponseCache();
        using var secondCache = new MemoryOnlyTransientResponseCache();
        var calls = 0;

        var first = await firstCache.GetOrCreateAsync(
            "lookup:key", TimeSpan.FromHours(1), _ => Task.FromResult(++calls));
        var second = await secondCache.GetOrCreateAsync(
            "lookup:key", TimeSpan.FromHours(1), _ => Task.FromResult(++calls));

        Assert.AreEqual(1, first);
        Assert.AreEqual(2, second);
        Assert.AreEqual(2, calls);
    }
}

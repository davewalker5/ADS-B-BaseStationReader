using BaseStationReader.Data;
using BaseStationReader.Entities.Config;
using BaseStationReader.Entities.Events;
using BaseStationReader.Entities.Hub;
using BaseStationReader.Entities.Tracking;
using BaseStationReader.Interfaces.Tracking;
using BaseStationReader.TrackerHub.Services;
using Microsoft.EntityFrameworkCore;

namespace BaseStationReader.Tests.TrackerHub;

[TestClass]
public class ObservationSessionEditorServiceTest
{
    [TestMethod]
    public async Task SaveUpdatesOnlySessionNotesTest()
    {
        var factory = CreateContextFactory();
        var sessionId = await AddSessionAsync(factory);
        var runtime = CreateRuntime();
        var service = new ObservationSessionEditorService(factory, runtime);

        await service.SaveNotesAsync(sessionId, "  Updated notes  ");

        var saved = await service.GetAsync(sessionId);
        Assert.IsNotNull(saved);
        Assert.AreEqual("Updated notes", saved.Notes);
        Assert.AreEqual("receiver.local", saved.Host);
        Assert.AreEqual(30003, saved.Port);
        Assert.AreEqual("Test profile", saved.ProfileName);
    }

    [TestMethod]
    public async Task SaveRejectsChangesWhileSessionIsActiveTest()
    {
        var factory = CreateContextFactory();
        var sessionId = await AddSessionAsync(factory);
        var runtime = CreateRuntime();
        using var cancellation = new CancellationTokenSource();
        var runtimeTask = runtime.StartAsync(cancellation.Token);
        await runtime.StartTrackingAsync("receiver.local", 30003);
        var service = new ObservationSessionEditorService(factory, runtime);

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => service.SaveNotesAsync(sessionId, "Changed while active"));

        cancellation.Cancel();
        await runtimeTask.WaitAsync(TimeSpan.FromSeconds(2));
    }

    private static TrackingRuntime CreateRuntime() => new(
        new TrackerApplicationSettings { Host = "receiver.local", Port = 30003, TrackedBehaviours = [] },
        (settings, _) => new WaitingController(settings));

    private static InMemoryContextFactory CreateContextFactory()
    {
        var options = new DbContextOptionsBuilder<BaseStationReaderDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new InMemoryContextFactory(options);
    }

    private static async Task<int> AddSessionAsync(InMemoryContextFactory factory)
    {
        await using var context = factory.CreateDbContext();
        var session = new ObservationSession
        {
            StartedAtUtc = DateTime.UtcNow,
            ProfileName = "Test profile",
            Host = "receiver.local",
            Port = 30003,
            IncludedBehaviours = "Unknown",
            Notes = "Original notes"
        };
        await context.ObservationSessions.AddAsync(session);
        await context.SaveChangesAsync();
        return session.Id;
    }

    private sealed class InMemoryContextFactory(DbContextOptions<BaseStationReaderDbContext> options)
        : IDbContextFactory<BaseStationReaderDbContext>
    {
        public BaseStationReaderDbContext CreateDbContext() => new(options);
    }

    private sealed class WaitingController(TrackerApplicationSettings settings) : ITrackerController
    {
        public event EventHandler<AircraftNotificationEventArgs> AircraftEvent { add { } remove { } }
        public IEnumerable<TrackedAircraftDto> State => [];
        public TrackingOptions TrackingOptions => TrackingOptions.FromTrackerSettings(settings);
        public int QueueSize => 0;

        public async Task StartAsync(CancellationToken token)
        {
            try { await Task.Delay(Timeout.InfiniteTimeSpan, token); }
            catch (OperationCanceledException) when (token.IsCancellationRequested) { }
        }

        public Task FlushQueueAsync() => Task.CompletedTask;
    }
}

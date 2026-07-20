using BaseStationReader.Entities.Config;
using BaseStationReader.Entities.Events;
using BaseStationReader.Entities.Hub;
using BaseStationReader.Interfaces.Tracking;
using BaseStationReader.TrackerHub.Services;

namespace BaseStationReader.Tests.TrackerHub;

[TestClass]
public class TrackingRuntimeTest
{
    [TestMethod]
    public async Task ApplyStopsCurrentControllerAndStartsReplacementTest()
    {
        var controllers = new List<FakeController>();
        var initial = Settings("Initial.json");
        var runtime = new TrackingRuntime(initial, settings =>
        {
            var controller = new FakeController(settings);
            controllers.Add(controller);
            return controller;
        });
        using var cancellation = new CancellationTokenSource();
        var runtimeTask = runtime.StartAsync(cancellation.Token);
        await WaitUntilAsync(() => controllers.Count == 1 && controllers[0].Started);

        await runtime.ApplyAsync(Settings("Replacement.json"));

        Assert.HasCount(2, controllers);
        Assert.IsTrue(controllers[0].Stopped);
        Assert.IsTrue(controllers[1].Started);
        Assert.AreEqual("Replacement.json", runtime.TrackingOptions.TrackingProfile);

        cancellation.Cancel();
        await runtimeTask.WaitAsync(TimeSpan.FromSeconds(2));
        Assert.IsTrue(controllers[1].Stopped);
    }

    private static TrackerApplicationSettings Settings(string profile) => new()
    {
        TrackingProfile = profile,
        TrackedBehaviours = []
    };

    private static async Task WaitUntilAsync(Func<bool> predicate)
    {
        var timeout = DateTime.UtcNow.AddSeconds(2);
        while (!predicate() && DateTime.UtcNow < timeout) await Task.Delay(10);
        Assert.IsTrue(predicate());
    }

    private sealed class FakeController(TrackerApplicationSettings settings) : ITrackerController
    {
        public event EventHandler<AircraftNotificationEventArgs> AircraftEvent { add { } remove { } }
        public IEnumerable<TrackedAircraftDto> State => [];
        public TrackingOptions TrackingOptions => TrackingOptions.FromTrackerSettings(settings);
        public int QueueSize => 0;
        public bool Started { get; private set; }
        public bool Stopped { get; private set; }

        public async Task StartAsync(CancellationToken token)
        {
            Started = true;
            try { await Task.Delay(Timeout.InfiniteTimeSpan, token); }
            catch (OperationCanceledException) when (token.IsCancellationRequested) { }
            Stopped = true;
        }

        public Task FlushQueueAsync() => Task.CompletedTask;
    }
}

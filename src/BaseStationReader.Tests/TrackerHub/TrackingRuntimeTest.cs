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
    public async Task ApplyRejectsProfileChangeDuringActiveSessionTest()
    {
        var controllers = new List<FakeController>();
        var initial = Settings("Initial.json");
        var runtime = new TrackingRuntime(initial, (settings, _) =>
        {
            var controller = new FakeController(settings);
            controllers.Add(controller);
            return controller;
        });
        using var cancellation = new CancellationTokenSource();
        var runtimeTask = runtime.StartAsync(cancellation.Token);
        await runtime.StartTrackingAsync();
        await WaitUntilAsync(() => controllers.Count == 1 && controllers[0].Started);

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => runtime.ApplyAsync(Settings("Replacement.json")));

        Assert.HasCount(1, controllers);
        Assert.IsFalse(controllers[0].Stopped);
        Assert.AreEqual("Initial.json", runtime.TrackingOptions.TrackingProfile);

        cancellation.Cancel();
        await runtimeTask.WaitAsync(TimeSpan.FromSeconds(2));
        Assert.IsTrue(controllers[0].Stopped);
    }

    [TestMethod]
    public async Task RuntimeWaitsForSessionStartAndAllowsManualControlTest()
    {
        var controllers = new List<FakeController>();
        var runtime = new TrackingRuntime(Settings("Initial.json"), (settings, _) =>
        {
            var controller = new FakeController(settings);
            controllers.Add(controller);
            return controller;
        });
        using var cancellation = new CancellationTokenSource();
        var runtimeTask = runtime.StartAsync(cancellation.Token);

        await Task.Delay(25);
        Assert.IsEmpty(controllers);
        Assert.IsFalse(runtime.IsTracking);

        await runtime.StartTrackingAsync();
        await WaitUntilAsync(() => controllers.Count == 1 && controllers[0].Started);
        Assert.IsTrue(runtime.IsTracking);

        await runtime.StopTrackingAsync();
        Assert.IsFalse(runtime.IsTracking);
        Assert.IsTrue(controllers[0].Stopped);

        await runtime.StartTrackingAsync();
        await WaitUntilAsync(() => controllers.Count == 2 && controllers[1].Started);

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

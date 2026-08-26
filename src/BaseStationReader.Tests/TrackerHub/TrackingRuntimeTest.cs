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
    public async Task StartRejectsInvalidSessionNameTest()
    {
        var runtime = new TrackingRuntime(Settings("Initial.json"), (settings, _, _) => new FakeController(settings));
        using var cancellation = new CancellationTokenSource();
        var runtimeTask = runtime.StartAsync(cancellation.Token);

        await Assert.ThrowsExactlyAsync<ArgumentException>(
            () => runtime.StartTrackingAsync("receiver.local", 30003, " "));
        await Assert.ThrowsExactlyAsync<ArgumentException>(
            () => runtime.StartTrackingAsync("receiver.local", 30003, new string('x', 101)));

        cancellation.Cancel();
        await runtimeTask.WaitAsync(TimeSpan.FromSeconds(2));
    }

    [TestMethod]
    public async Task ApplyRejectsProfileChangeDuringActiveSessionTest()
    {
        var controllers = new List<FakeController>();
        var initial = Settings("Initial.json");
        var runtime = new TrackingRuntime(initial, (settings, _, _) =>
        {
            var controller = new FakeController(settings);
            controllers.Add(controller);
            return controller;
        });
        using var cancellation = new CancellationTokenSource();
        var runtimeTask = runtime.StartAsync(cancellation.Token);
        await runtime.StartTrackingAsync("initial.receiver", 30003, "Initial session");
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
        var runtime = new TrackingRuntime(Settings("Initial.json"), (settings, _, _) =>
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

        await runtime.StartTrackingAsync("first.receiver", 30003, "First session");
        await WaitUntilAsync(() => controllers.Count == 1 && controllers[0].Started);
        Assert.IsTrue(runtime.IsTracking);

        await runtime.StopTrackingAsync();
        Assert.IsFalse(runtime.IsTracking);
        Assert.IsTrue(controllers[0].Stopped);

        await runtime.StartTrackingAsync("second.receiver", 30004, "Second session");
        await WaitUntilAsync(() => controllers.Count == 2 && controllers[1].Started);
        Assert.AreEqual("second.receiver", runtime.TrackingOptions.ReceiverHost);
        Assert.AreEqual(30004, runtime.TrackingOptions.ReceiverPort);
        Assert.AreEqual("second.receiver", controllers[1].TrackingOptions.ReceiverHost);
        Assert.AreEqual(30004, controllers[1].TrackingOptions.ReceiverPort);

        cancellation.Cancel();
        await runtimeTask.WaitAsync(TimeSpan.FromSeconds(2));
        Assert.IsTrue(controllers[1].Stopped);
    }

    [TestMethod]
    public async Task RawMessageUpdatesAndResetsSessionActivityTest()
    {
        var controllers = new List<FakeController>();
        var runtime = new TrackingRuntime(Settings("Initial.json"), (settings, _, _) =>
        {
            var controller = new FakeController(settings);
            controllers.Add(controller);
            return controller;
        });
        using var cancellation = new CancellationTokenSource();
        var runtimeTask = runtime.StartAsync(cancellation.Token);

        await runtime.StartTrackingAsync("receiver.local", 30003, "First session");
        await WaitUntilAsync(() => controllers.Count == 1 && controllers[0].Started);
        Assert.IsNull(runtime.LastActivityUtc);

        var beforeMessage = DateTime.UtcNow;
        controllers[0].EmitMessage();
        Assert.IsNotNull(runtime.LastActivityUtc);
        Assert.IsGreaterThanOrEqualTo(beforeMessage, runtime.LastActivityUtc.Value);

        await runtime.StopTrackingAsync();
        Assert.IsNotNull(runtime.LastActivityUtc);
        await runtime.StartTrackingAsync("receiver.local", 30003, "Second session");
        await WaitUntilAsync(() => controllers.Count == 2 && controllers[1].Started);
        Assert.IsNull(runtime.LastActivityUtc);

        cancellation.Cancel();
        await runtimeTask.WaitAsync(TimeSpan.FromSeconds(2));
    }

    [TestMethod]
    public async Task StopPassesHubFlushOverrideToControllerTest()
    {
        FakeController controller = null;
        var runtime = new TrackingRuntime(Settings("Initial.json"), (settings, _, _) =>
            controller = new FakeController(settings));
        using var cancellation = new CancellationTokenSource();
        var runtimeTask = runtime.StartAsync(cancellation.Token);
        await runtime.StartTrackingAsync("receiver.local", 30003, "Flush override session");
        await WaitUntilAsync(() => controller?.Started == true);

        await runtime.StopTrackingAsync(flushQueue: false);

        Assert.IsNotNull(controller);
        Assert.IsFalse(controller.FlushQueueOnStop.Value);
        Assert.IsTrue(controller.StopRequested);
        cancellation.Cancel();
        await runtimeTask.WaitAsync(TimeSpan.FromSeconds(2));
    }

    [TestMethod]
    public async Task RetainedQueueStopHasBoundedWaitTest()
    {
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var settings = Settings("Initial.json");
        settings.StopTimeout = 50;
        var controller = new BlockingController(settings, release.Task);
        var runtime = new TrackingRuntime(settings, (_, _, _) => controller);
        using var cancellation = new CancellationTokenSource();
        var runtimeTask = runtime.StartAsync(cancellation.Token);
        await runtime.StartTrackingAsync("receiver.local", 30003, "Bounded stop session");
        await WaitUntilAsync(() => controller.Started);

        var exception = await Assert.ThrowsExactlyAsync<TimeoutException>(
            () => runtime.StopTrackingAsync(flushQueue: false));

        Assert.IsTrue(controller.StopRequested);
        StringAssert.Contains(exception.Message, "50 ms");

        release.SetResult();
        await runtime.StopTrackingAsync(flushQueue: false).WaitAsync(TimeSpan.FromSeconds(2));
        cancellation.Cancel();
        await runtimeTask.WaitAsync(TimeSpan.FromSeconds(2));
    }

    [TestMethod]
    public async Task FaultedControllerIsReleasedWhenStopSurfacesFailureTest()
    {
        var controller = new FaultingController(Settings("Initial.json"));
        var runtime = new TrackingRuntime(Settings("Initial.json"), (_, _, _) => controller);
        using var cancellation = new CancellationTokenSource();
        var runtimeTask = runtime.StartAsync(cancellation.Token);
        await runtime.StartTrackingAsync("receiver.local", 30003, "Faulted stop session");
        await WaitUntilAsync(() => controller.Started);

        var exception = await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => runtime.StopTrackingAsync(flushQueue: true));

        Assert.AreEqual("Flush failed", exception.Message);
        Assert.IsFalse(runtime.IsTracking);
        cancellation.Cancel();
        await runtimeTask.WaitAsync(TimeSpan.FromSeconds(2));
    }

    [TestMethod]
    public async Task SessionResetCompletesBeforeControllerCanStartTest()
    {
        var order = new List<string>();
        var runtime = new TrackingRuntime(Settings("Initial.json"), (settings, _, _) =>
            new OrderingController(settings, order));
        using var cancellation = new CancellationTokenSource();
        var runtimeTask = runtime.StartAsync(cancellation.Token);

        await runtime.StartTrackingAsync(
            "receiver.local",
            30003,
            "Ordered session",
            beforeStart: (options, _) =>
            {
                order.Add($"reset:{options.ReceiverHost}:{options.ReceiverPort}");
                return ValueTask.CompletedTask;
            });
        await WaitUntilAsync(() => order.Contains("controller"));

        CollectionAssert.AreEqual(
            new[] { "reset:receiver.local:30003", "controller" },
            order);

        cancellation.Cancel();
        await runtimeTask.WaitAsync(TimeSpan.FromSeconds(2));
    }

    private static TrackerApplicationSettings Settings(string profile) => new()
    {
        TrackingProfile = profile,
        Host = "appsettings.receiver",
        Port = 30003,
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
        public event EventHandler<MessageReadEventArgs> MessageReceived;
        public IEnumerable<TrackedAircraftDto> State => [];
        public TrackingOptions TrackingOptions => TrackingOptions.FromTrackerSettings(settings);
        public int QueueSize => 0;
        public bool Started { get; private set; }
        public bool Stopped { get; private set; }
        public bool? FlushQueueOnStop { get; private set; }
        public bool StopRequested { get; private set; }

        public async Task StartAsync(CancellationToken token)
        {
            Started = true;
            try { await Task.Delay(Timeout.InfiniteTimeSpan, token); }
            catch (OperationCanceledException) when (token.IsCancellationRequested) { }
            Stopped = true;
        }

        public Task FlushQueueAsync(CancellationToken cancellationToken = default,
            IProgress<BaseStationReader.Entities.Spool.QueueFlushProgress> progress = null) => Task.CompletedTask;

        public void ConfigureStopFlush(bool flushQueue, CancellationToken cancellationToken = default,
            IProgress<BaseStationReader.Entities.Spool.QueueFlushProgress> progress = null)
            => FlushQueueOnStop = flushQueue;

        public void RequestStop() => StopRequested = true;

        public void EmitMessage()
            => MessageReceived?.Invoke(this, new MessageReadEventArgs { Message = "MSG" });
    }

    private sealed class BlockingController(
        TrackerApplicationSettings settings,
        Task release) : ITrackerController
    {
        public event EventHandler<AircraftNotificationEventArgs> AircraftEvent { add { } remove { } }
        public IEnumerable<TrackedAircraftDto> State => [];
        public TrackingOptions TrackingOptions => TrackingOptions.FromTrackerSettings(settings);
        public int QueueSize => 1;
        public bool Started { get; private set; }
        public bool StopRequested { get; private set; }

        public async Task StartAsync(CancellationToken token)
        {
            Started = true;
            await release;
        }

        public void RequestStop() => StopRequested = true;

        public Task FlushQueueAsync(CancellationToken cancellationToken = default,
            IProgress<BaseStationReader.Entities.Spool.QueueFlushProgress> progress = null) => Task.CompletedTask;
    }

    private sealed class OrderingController(
        TrackerApplicationSettings settings,
        List<string> order) : ITrackerController
    {
        public event EventHandler<AircraftNotificationEventArgs> AircraftEvent { add { } remove { } }
        public IEnumerable<TrackedAircraftDto> State => [];
        public TrackingOptions TrackingOptions => TrackingOptions.FromTrackerSettings(settings);
        public int QueueSize => 0;

        public async Task StartAsync(CancellationToken token)
        {
            order.Add("controller");
            try { await Task.Delay(Timeout.InfiniteTimeSpan, token); }
            catch (OperationCanceledException) when (token.IsCancellationRequested) { }
        }

        public Task FlushQueueAsync(CancellationToken cancellationToken = default,
            IProgress<BaseStationReader.Entities.Spool.QueueFlushProgress> progress = null) => Task.CompletedTask;
    }

    private sealed class FaultingController(TrackerApplicationSettings settings) : ITrackerController
    {
        private readonly TaskCompletionSource _stopping = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public event EventHandler<AircraftNotificationEventArgs> AircraftEvent { add { } remove { } }
        public IEnumerable<TrackedAircraftDto> State => [];
        public TrackingOptions TrackingOptions => TrackingOptions.FromTrackerSettings(settings);
        public int QueueSize => 1;
        public bool Started { get; private set; }

        public async Task StartAsync(CancellationToken token)
        {
            Started = true;
            await _stopping.Task;
            throw new InvalidOperationException("Flush failed");
        }

        public void RequestStop() => _stopping.TrySetResult();

        public Task FlushQueueAsync(CancellationToken cancellationToken = default,
            IProgress<BaseStationReader.Entities.Spool.QueueFlushProgress> progress = null) => Task.CompletedTask;
    }
}

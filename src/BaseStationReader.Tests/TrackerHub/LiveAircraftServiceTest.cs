#nullable enable

using BaseStationReader.Entities.Hub;
using BaseStationReader.Entities.Events;
using BaseStationReader.Interfaces.Tracking;
using BaseStationReader.TrackerHub.Services;
using Microsoft.Extensions.Configuration;

namespace BaseStationReader.Tests.TrackerHub;

[TestClass]
public sealed class LiveAircraftServiceTest
{
    [TestMethod]
    public async Task BurstUpdatesProduceOneCoalescedStateChangeTest()
    {
        await using var service = new LiveAircraftService(
            new FakeController(),
            new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ApplicationSettings:RefreshInterval"] = "100"
                })
                .Build());
        var notifications = 0;
        service.StateChanged += (_, _) => Interlocked.Increment(ref notifications);

        for (var index = 0; index < 100; index++)
        {
            service.ApplyUpdate(new TrackedAircraftDto { Address = index.ToString("X6") });
        }

        Assert.HasCount(100, service.Aircraft);
        Assert.AreEqual(0, Volatile.Read(ref notifications));
        await WaitUntilAsync(() => Volatile.Read(ref notifications) == 1);
        await Task.Delay(150);
        Assert.AreEqual(1, Volatile.Read(ref notifications));
    }

    [TestMethod]
    public async Task InProcessControllerEventUpdatesLiveStateTest()
    {
        var controller = new FakeController();
        await using var service = new LiveAircraftService(
            controller,
            new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ApplicationSettings:RefreshInterval"] = "100"
                })
                .Build());

        await service.StartAsync();
        controller.Publish(new TrackedAircraftDto { Address = "ABC123" });

        Assert.AreEqual(ConnectionState.Connected, service.ConnectionState);
        Assert.AreEqual("ABC123", service.Aircraft.Single().Address);
    }

    private static async Task WaitUntilAsync(Func<bool> predicate)
    {
        var timeout = DateTime.UtcNow.AddSeconds(2);
        while (!predicate() && DateTime.UtcNow < timeout)
        {
            await Task.Delay(10);
        }

        Assert.IsTrue(predicate());
    }

    private sealed class FakeController : ITrackerController
    {
        public event EventHandler<AircraftNotificationEventArgs>? AircraftEvent;
        public IEnumerable<TrackedAircraftDto> State => [];
        public TrackingOptions TrackingOptions { get; } = new();
        public int QueueSize => 0;

        public void Publish(TrackedAircraftDto aircraft)
        {
            AircraftEvent?.Invoke(this, new AircraftNotificationEventArgs
            {
                Aircraft = new BaseStationReader.Entities.Tracking.TrackedAircraft
                {
                    Address = aircraft.Address
                },
                NotificationType = AircraftNotificationType.Updated
            });
        }

        public Task StartAsync(CancellationToken token) => Task.CompletedTask;
        public Task FlushQueueAsync() => Task.CompletedTask;
    }
}

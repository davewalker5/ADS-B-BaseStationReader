using BaseStationReader.BusinessLogic.Tracking;
using BaseStationReader.Entities.History;
using BaseStationReader.Entities.Tracking;
using BaseStationReader.Interfaces.Tracking;

namespace BaseStationReader.Tests.Tracking;

[TestClass]
public sealed class PositionDensitySnapshotOrchestratorTest
{
    /// <summary>
    /// Verifies recorded positions produce a complete periodic in-memory snapshot.
    /// </summary>
    [TestMethod]
    public async Task PeriodicallyUpdatesSnapshotTestAsync()
    {
        var stateManager = new PositionDensitySnapshotStateManager(new PositionDensitySnapshotMerger());
        IPositionDensitySnapshotOrchestrator orchestrator = new PositionDensitySnapshotOrchestrator(
            new PositionDensityAggregator(),
            stateManager);
        using var cancellation = new CancellationTokenSource();

        orchestrator.Start(
            42,
            new PositionDensityBounds(50d, 52d, -1d, 1d),
            TimeSpan.FromMilliseconds(20),
            (_, _) => { },
            cancellation.Token);
        orchestrator.Record(new AircraftPosition { Latitude = 51.1m, Longitude = -0.2m });
        orchestrator.Record(new AircraftPosition { Latitude = 51.1m, Longitude = -0.2m });

        var snapshot = await WaitForSnapshotAsync(stateManager, 42);
        await orchestrator.StopAsync();

        Assert.AreEqual(2, snapshot.PositionCount);
        Assert.HasCount(1, snapshot.Bins);
        Assert.AreEqual(2, snapshot.Bins[0].Count);
    }

    /// <summary>
    /// Verifies invalid observations are excluded from periodic snapshots.
    /// </summary>
    [TestMethod]
    public async Task IgnoresInvalidPositionsTestAsync()
    {
        var stateManager = new PositionDensitySnapshotStateManager(new PositionDensitySnapshotMerger());
        IPositionDensitySnapshotOrchestrator orchestrator = new PositionDensitySnapshotOrchestrator(
            new PositionDensityAggregator(),
            stateManager);
        using var cancellation = new CancellationTokenSource();

        orchestrator.Start(
            42,
            new PositionDensityBounds(50d, 52d, -1d, 1d),
            TimeSpan.FromMilliseconds(20),
            (_, _) => { },
            cancellation.Token);
        orchestrator.Record(new AircraftPosition { Latitude = 91m, Longitude = 0m });

        var snapshot = await WaitForSnapshotAsync(stateManager, 42);
        await orchestrator.StopAsync();

        Assert.AreEqual(0, snapshot.PositionCount);
        Assert.IsEmpty(snapshot.Bins);
    }

    /// <summary>
    /// Waits for the periodic loop to publish a snapshot within a bounded test interval.
    /// </summary>
    /// <param name="stateManager"></param>
    /// <param name="sessionId"></param>
    /// <returns></returns>
    private static async Task<PositionDensity> WaitForSnapshotAsync(
        IPositionDensitySnapshotStateManager stateManager,
        int sessionId)
    {
        // Polling keeps the test independent of the timer's exact scheduler wake-up time.
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        while (!timeout.IsCancellationRequested)
        {
            var snapshot = stateManager.GetSnapshot(sessionId);
            if (snapshot is not null)
            {
                return snapshot;
            }
            await Task.Delay(10, timeout.Token);
        }
        throw new AssertFailedException("The periodic snapshot was not published.");
    }
}

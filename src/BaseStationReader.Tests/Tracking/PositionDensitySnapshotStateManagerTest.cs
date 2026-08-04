using BaseStationReader.BusinessLogic.Tracking;
using BaseStationReader.Entities.History;
using BaseStationReader.Interfaces.Tracking;

namespace BaseStationReader.Tests.Tracking;

[TestClass]
public sealed class PositionDensitySnapshotStateManagerTest
{
    /// <summary>
    /// Verifies refreshed values accumulate in business-logic-owned state.
    /// </summary>
    [TestMethod]
    public void MergeMaintainsCurrentSessionSnapshotTest()
    {
        IPositionDensitySnapshotStateManager manager = CreateManager();
        manager.Merge(Density(12, 2));

        var snapshot = manager.Merge(Density(12, 4));

        Assert.AreEqual(12, snapshot.SessionId);
        Assert.AreEqual(4, snapshot.PositionCount);
        Assert.AreSame(snapshot, manager.GetSnapshot(12));
    }

    /// <summary>
    /// Verifies a new session replaces state and cannot expose the previous session snapshot.
    /// </summary>
    [TestMethod]
    public void MergeReplacesSnapshotWhenSessionChangesTest()
    {
        IPositionDensitySnapshotStateManager manager = CreateManager();
        manager.Merge(Density(12, 2));

        var snapshot = manager.Merge(Density(13, 1));

        Assert.AreEqual(13, snapshot.SessionId);
        Assert.IsNull(manager.GetSnapshot(12));
        Assert.AreSame(snapshot, manager.GetSnapshot(13));
    }

    /// <summary>
    /// Verifies clearing removes the current in-memory snapshot.
    /// </summary>
    [TestMethod]
    public void ClearRemovesSnapshotTest()
    {
        IPositionDensitySnapshotStateManager manager = CreateManager();
        manager.Merge(Density(12, 2));

        manager.Clear();

        Assert.IsNull(manager.GetSnapshot(12));
    }

    /// <summary>
    /// Creates a snapshot-state manager with the production merger.
    /// </summary>
    /// <returns></returns>
    private static IPositionDensitySnapshotStateManager CreateManager()
    {
        // The production merger keeps these tests aligned with the live tracker registration.
        return new PositionDensitySnapshotStateManager(new PositionDensitySnapshotMerger());
    }

    /// <summary>
    /// Creates one density calculation for state-manager tests.
    /// </summary>
    /// <param name="sessionId"></param>
    /// <param name="positionCount"></param>
    /// <returns></returns>
    private static PositionDensity Density(int sessionId, int positionCount)
    {
        // A stable bin identity allows successive calculations to exercise monotonic merging.
        return new PositionDensity
        {
            SessionId = sessionId,
            PositionCount = positionCount,
            MaximumBinCount = positionCount,
            MinimumLatitude = 50d,
            MaximumLatitude = 52d,
            MinimumLongitude = -1d,
            MaximumLongitude = 1d,
            Bins =
            [
                new PositionDensityBin { Latitude = 51d, Longitude = 0d, Count = positionCount }
            ]
        };
    }
}

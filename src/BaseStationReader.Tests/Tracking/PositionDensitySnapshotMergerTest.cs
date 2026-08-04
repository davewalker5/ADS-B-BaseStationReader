using BaseStationReader.Entities.History;
using BaseStationReader.BusinessLogic.Tracking;

namespace BaseStationReader.Tests.Tracking;

[TestClass]
public class PositionDensitySnapshotMergerTest
{
    [TestMethod]
    public void MergeRetainsMissingBinsAndOnlyIncreasesCountsTest()
    {
        var current = Density(28, 12,
            new PositionDensityBinDto { Latitude = 51.5, Longitude = -0.2, Count = 4 },
            new PositionDensityBinDto { Latitude = 51.6, Longitude = -0.1, Count = 2 });
        var refreshed = Density(28, 11,
            new PositionDensityBinDto { Latitude = 51.5, Longitude = -0.2, Count = 3 },
            new PositionDensityBinDto { Latitude = 51.7, Longitude = 0.0, Count = 5 });

        var merged = new PositionDensitySnapshotMerger().Merge(current, refreshed);

        Assert.IsNotNull(merged);
        Assert.AreEqual(12, merged.PositionCount);
        Assert.HasCount(3, merged.Bins);
        Assert.AreEqual(4, merged.Bins.Single(bin => bin.Latitude == 51.5).Count);
        Assert.AreEqual(2, merged.Bins.Single(bin => bin.Latitude == 51.6).Count);
        Assert.AreEqual(5, merged.Bins.Single(bin => bin.Latitude == 51.7).Count);
    }

    [TestMethod]
    public void MergeReplacesSnapshotWhenSessionChangesTest()
    {
        var current = Density(28, 12,
            new PositionDensityBinDto { Latitude = 51.5, Longitude = -0.2, Count = 4 });
        var refreshed = Density(29, 1,
            new PositionDensityBinDto { Latitude = 50.0, Longitude = 1.0, Count = 1 });

        var merged = new PositionDensitySnapshotMerger().Merge(current, refreshed);

        Assert.AreSame(refreshed, merged);
        Assert.HasCount(1, merged!.Bins);
    }

    private static PositionDensityDto Density(
        int sessionId,
        int positionCount,
        params PositionDensityBinDto[] bins)
        => new()
        {
            SessionId = sessionId,
            PositionCount = positionCount,
            MaximumBinCount = bins.Length == 0 ? 0 : bins.Max(bin => bin.Count),
            MinimumLatitude = 47,
            MaximumLatitude = 56,
            MinimumLongitude = -8,
            MaximumLongitude = 6,
            Bins = bins
        };
}

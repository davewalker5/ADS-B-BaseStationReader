using BaseStationReader.BusinessLogic.Tracking;
using BaseStationReader.Entities.History;
using BaseStationReader.Interfaces.Tracking;

namespace BaseStationReader.Tests.Tracking;

[TestClass]
public sealed class PositionDensitySnapshotMapperTest
{
    /// <summary>
    /// Verifies every reconstruction value is copied into an independent persistence entity.
    /// </summary>
    [TestMethod]
    public void MapsCompleteSnapshotTest()
    {
        var capturedAtUtc = DateTime.UtcNow;
        var source = new PositionDensity
        {
            SessionId = 42,
            PositionCount = 3,
            MaximumBinCount = 2,
            MinimumLatitude = 50d,
            MaximumLatitude = 52d,
            MinimumLongitude = -1d,
            MaximumLongitude = 1d,
            Bins =
            [
                new PositionDensityBin { Latitude = 51.1d, Longitude = -0.2d, Count = 2 }
            ]
        };
        IPositionDensitySnapshotMapper mapper = new PositionDensitySnapshotMapper();

        var entity = mapper.Map(source, capturedAtUtc);

        Assert.AreEqual(42, entity.SessionId);
        Assert.AreEqual(capturedAtUtc, entity.CapturedAtUtc);
        Assert.AreEqual(3, entity.PositionCount);
        Assert.AreEqual(2, entity.MaximumBinCount);
        Assert.HasCount(1, entity.Cells);
        Assert.AreEqual(51.1d, entity.Cells.Single().Latitude);
        Assert.AreEqual(2, entity.Cells.Single().Count);
    }
}

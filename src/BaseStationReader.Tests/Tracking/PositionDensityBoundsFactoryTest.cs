using BaseStationReader.BusinessLogic.Tracking;

namespace BaseStationReader.Tests.Tracking;

[TestClass]
public sealed class PositionDensityBoundsFactoryTest
{
    [TestMethod]
    public void NullMaximumDistanceDefaultsToFiftyNauticalMilesTest()
    {
        var bounds = PositionDensityBoundsFactory.Create(0d, 0d, null);

        Assert.AreEqual(-50d / 60d, bounds.MinimumLatitude);
        Assert.AreEqual(50d / 60d, bounds.MaximumLatitude);
        Assert.AreEqual(-50d / 60d, bounds.MinimumLongitude);
        Assert.AreEqual(50d / 60d, bounds.MaximumLongitude);
    }

    [TestMethod]
    public void ConfiguredMaximumDistanceOverridesDefaultTest()
    {
        var bounds = PositionDensityBoundsFactory.Create(0d, 0d, 15);

        Assert.AreEqual(-15d / 60d, bounds.MinimumLatitude);
        Assert.AreEqual(15d / 60d, bounds.MaximumLatitude);
        Assert.AreEqual(-15d / 60d, bounds.MinimumLongitude);
        Assert.AreEqual(15d / 60d, bounds.MaximumLongitude);
    }
}

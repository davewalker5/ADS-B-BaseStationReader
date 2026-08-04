using BaseStationReader.BusinessLogic.Tracking;
using BaseStationReader.BusinessLogic.Geometry;
using BaseStationReader.Entities.History;

namespace BaseStationReader.Tests.TrackerHub;

[TestClass]
public class FlightProfileBuilderTest
{
    /// <summary>
    /// Verifies chronological ordering, sequencing, and summary preparation.
    /// </summary>
    [TestMethod]
    public void BuildOrdersAndSummarisesProfile()
    {
        var builder = new FlightProfileBuilder(null, null, new GeographicCalculator());
        var start = new DateTime(2026, 7, 17, 10, 0, 0, DateTimeKind.Utc);
        FlightProfilePoint[] points =
        [
            new() { Timestamp = start.AddMinutes(2), Altitude = 3000, Distance = 8 },
            new() { Timestamp = start, Altitude = 1000, Distance = 12 },
            new() { Timestamp = start.AddMinutes(1), Altitude = 2000, Distance = 10 }
        ];

        // Build from deliberately unordered input to exercise plot preparation rather than query ordering.
        var profile = builder.Build(42, "ABC123", "TEST1", points);

        Assert.HasCount(3, profile.Points);
        Assert.AreEqual(start, profile.Points[0].Timestamp);
        Assert.AreEqual(1, profile.Points[0].Sequence);
        Assert.AreEqual(3, profile.Points[2].Sequence);
        Assert.AreEqual(1000m, profile.InitialAltitude);
        Assert.AreEqual(3000m, profile.FinalAltitude);
        Assert.AreEqual(1000m, profile.MinimumAltitude);
        Assert.AreEqual(3000m, profile.MaximumAltitude);
        Assert.AreEqual(8d, profile.ClosestDistance);
        Assert.AreEqual(12d, profile.FurthestDistance);
        Assert.AreEqual(TimeSpan.FromMinutes(2), profile.Duration);
    }

    /// <summary>
    /// Verifies missing telemetry remains missing rather than becoming zero.
    /// </summary>
    [TestMethod]
    public void BuildHandlesMissingTelemetry()
    {
        var builder = new FlightProfileBuilder(null, null, new GeographicCalculator());
        var points = new[] { new FlightProfilePoint { Timestamp = DateTime.UtcNow } };

        // Prepare a point with no altitude, distance, or coordinates.
        var profile = builder.Build(7, "000001", string.Empty, points);

        Assert.IsNull(profile.InitialAltitude);
        Assert.IsNull(profile.FinalAltitude);
        Assert.IsNull(profile.MinimumAltitude);
        Assert.IsNull(profile.MaximumAltitude);
        Assert.IsNull(profile.ClosestDistance);
        Assert.IsNull(profile.FurthestDistance);
        Assert.IsNull(profile.Points[0].Bearing);
    }

    /// <summary>
    /// Verifies bearing enrichment uses the configured receiver coordinates.
    /// </summary>
    [TestMethod]
    public void BuildCalculatesBearingFromReceiver()
    {
        var builder = new FlightProfileBuilder(51.4700, -0.4543, new GeographicCalculator());
        var points = new[]
        {
            new FlightProfilePoint
            {
                Timestamp = DateTime.UtcNow,
                Latitude = 51.5000m,
                Longitude = -0.1000m,
                Altitude = 5000,
                Distance = 15
            }
        };

        // The test asserts a broad east-northeast bearing without coupling to display rounding.
        var profile = builder.Build(9, "ABC999", "TEST9", points);

        Assert.IsNotNull(profile.Points[0].Bearing);
        Assert.IsGreaterThan(70d, profile.Points[0].Bearing.Value);
        Assert.IsLessThan(90d, profile.Points[0].Bearing.Value);
    }
}

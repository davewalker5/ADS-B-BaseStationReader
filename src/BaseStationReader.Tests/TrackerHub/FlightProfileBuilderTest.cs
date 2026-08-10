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
        var timestamp = DateTime.UtcNow;
        FlightProfilePoint[] points =
        [
            new() { Timestamp = timestamp },
            new() { Timestamp = default, Altitude = 4000 },
            new() { Timestamp = timestamp.AddSeconds(1), Altitude = 5000 }
        ];

        // Rows without altitude or a real timestamp are ignored; coordinates and distance are optional for time plots.
        var profile = builder.Build(7, "000001", string.Empty, points);

        Assert.HasCount(1, profile.Points);
        Assert.AreEqual(5000m, profile.InitialAltitude);
        Assert.AreEqual(5000m, profile.FinalAltitude);
        Assert.AreEqual(5000m, profile.MinimumAltitude);
        Assert.AreEqual(5000m, profile.MaximumAltitude);
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

    /// <summary>
    /// Verifies the geographic outlier seen in flight paths is also excluded from both profile charts.
    /// </summary>
    [TestMethod]
    public void BuildRejectsIsolatedPositionSpike()
    {
        var builder = new FlightProfileBuilder(null, null, new GeographicCalculator());
        var timestamp = new DateTime(2026, 8, 10, 10, 40, 0, DateTimeKind.Utc);
        FlightProfilePoint[] points =
        [
            new() { Timestamp = timestamp, Latitude = 51.66540m, Longitude = -0.82954m, Altitude = 20775, Distance = 16 },
            new() { Timestamp = timestamp.AddSeconds(1), Latitude = -46.01083m, Longitude = 98.18703m, Altitude = 20775, Distance = 7771 },
            new() { Timestamp = timestamp.AddSeconds(2), Latitude = 51.66165m, Longitude = -0.83296m, Altitude = 20800, Distance = 16 }
        ];

        var profile = builder.Build(10, "407E82", "RUK9NE", points);

        Assert.HasCount(2, profile.Points);
        Assert.IsFalse(profile.Points.Any(point => point.Distance == 7771));
        Assert.AreEqual(16d, profile.FurthestDistance);
    }

    /// <summary>
    /// Verifies an isolated altitude excursion is removed while a sustained receiver update remains visible.
    /// </summary>
    [TestMethod]
    public void BuildRejectsIsolatedAltitudeSpikeButRetainsSustainedStep()
    {
        var builder = new FlightProfileBuilder(null, null, new GeographicCalculator());
        var timestamp = new DateTime(2026, 8, 10, 10, 40, 0, DateTimeKind.Utc);
        FlightProfilePoint[] points =
        [
            Point(timestamp, 0, 4325),
            Point(timestamp, 1, 114500),
            Point(timestamp, 2, 4325),
            Point(timestamp, 3, 6500),
            Point(timestamp, 4, 6500),
            Point(timestamp, 5, 6500)
        ];

        var profile = builder.Build(11, "ABED10", "FDX5184", points);

        Assert.HasCount(5, profile.Points);
        Assert.AreEqual(4325m, profile.MinimumAltitude);
        Assert.AreEqual(6500m, profile.MaximumAltitude);
        Assert.IsFalse(profile.Points.Any(point => point.Altitude == 114500));
        Assert.AreEqual(3, profile.Points.Count(point => point.Altitude == 6500));
    }

    /// <summary>Creates a nearby complete profile observation for spike-filter tests.</summary>
    private static FlightProfilePoint Point(DateTime timestamp, int seconds, decimal altitude) => new()
    {
        Timestamp = timestamp.AddSeconds(seconds),
        Latitude = 51m - seconds * 0.0001m,
        Longitude = -1m - seconds * 0.0001m,
        Altitude = altitude,
        Distance = 10
    };
}

using BaseStationReader.BusinessLogic.Tracking;
using BaseStationReader.BusinessLogic.Geometry;
using BaseStationReader.Entities.History;

namespace BaseStationReader.Tests.TrackerHub;

[TestClass]
public class FlightPathBuilderTest
{
    /// <summary>
    /// Verifies notebook-compatible units, local projection, altitude range, and bounds.
    /// </summary>
    [TestMethod]
    public void BuildProjectsAndSummarisesPath()
    {
        var builder = new FlightPathBuilder(51.47, -0.454, new GeographicCalculator());
        var timestamp = new DateTime(2026, 7, 17, 12, 0, 0, DateTimeKind.Utc);
        FlightProfilePoint[] points =
        [
            new() { Timestamp = timestamp, Latitude = 51.47m, Longitude = -0.454m, Altitude = 1000, Distance = 4 },
            new() { Timestamp = timestamp.AddSeconds(30), Latitude = 51.48m, Longitude = -0.444m, Altitude = 2000, Distance = 5 }
        ];

        // Prepare the same complete telemetry required by the reporting notebook.
        var path = builder.Build(1, "ABC123", "TEST1", "G-TEST", "A320", "TST1", "Test Air", "AAA → BBB", points);

        Assert.HasCount(2, path.Points);
        Assert.AreEqual(0d, path.Points[0].LocalXMetres, 0.001);
        Assert.AreEqual(0d, path.Points[0].LocalYMetres, 0.001);
        Assert.AreEqual(304.8d, path.Points[0].AltitudeMetres, 0.001);
        Assert.IsGreaterThan(0d, path.Points[1].LocalXMetres);
        Assert.IsGreaterThan(0d, path.Points[1].LocalYMetres);
        Assert.AreEqual(1, path.SegmentCount);
        Assert.IsLessThan(51.47d, path.South!.Value);
        Assert.IsGreaterThan(51.48d, path.North!.Value);
        Assert.IsGreaterThan(path.MinimumAltitudeMetres, path.MaximumAltitudeMetres);
    }

    /// <summary>
    /// Verifies duplicate removal and segmentation across reception gaps.
    /// </summary>
    [TestMethod]
    public void BuildDeduplicatesAndSegmentsPath()
    {
        var builder = new FlightPathBuilder(null, null, new GeographicCalculator());
        var timestamp = DateTime.UtcNow;
        var duplicate = new FlightProfilePoint { Timestamp = timestamp, Latitude = 51m, Longitude = -1m, Altitude = 5000, Distance = 10 };
        FlightProfilePoint[] points =
        [
            duplicate,
            duplicate,
            new() { Timestamp = timestamp.AddSeconds(91), Latitude = 51.1m, Longitude = -0.9m, Altitude = 6000, Distance = 11 }
        ];

        // A 91-second gap exceeds the notebook's 90-second connector threshold.
        var path = builder.Build(2, "ABC124", string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, points);

        Assert.HasCount(2, path.Points);
        Assert.AreEqual(1, path.Points[0].Segment);
        Assert.AreEqual(2, path.Points[1].Segment);
        Assert.AreEqual(2, path.SegmentCount);
    }

    /// <summary>
    /// Verifies incomplete and geographically invalid positions are handled gracefully.
    /// </summary>
    [TestMethod]
    public void BuildRejectsInvalidPositions()
    {
        var builder = new FlightPathBuilder(null, null, new GeographicCalculator());
        FlightProfilePoint[] points =
        [
            new() { Timestamp = DateTime.UtcNow, Latitude = null, Longitude = -1, Altitude = 1000, Distance = 2 },
            new() { Timestamp = DateTime.UtcNow, Latitude = 91, Longitude = -1, Altitude = 1000, Distance = 2 }
        ];

        // The result retains identifying metadata so the page can show a useful empty state.
        var path = builder.Build(3, "ABC125", "TEST3", string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, points);

        Assert.IsEmpty(path.Points);
        Assert.AreEqual("ABC125", path.Address);
        Assert.IsNull(path.North);
        Assert.AreEqual(0, path.SegmentCount);
    }

    /// <summary>
    /// Verifies every plotting field named by the UI contract is required before a row reaches either renderer.
    /// </summary>
    [TestMethod]
    public void BuildRejectsRowsWithInvalidRequiredPlottingFields()
    {
        var builder = new FlightPathBuilder(null, null, new GeographicCalculator());
        var timestamp = new DateTime(2026, 8, 10, 10, 40, 0, DateTimeKind.Utc);
        FlightProfilePoint[] points =
        [
            new() { Timestamp = default, Latitude = 51, Longitude = -1, Altitude = 5000, Distance = 10 },
            new() { Timestamp = timestamp, Latitude = null, Longitude = -1, Altitude = 5000, Distance = 10 },
            new() { Timestamp = timestamp, Latitude = 51, Longitude = null, Altitude = 5000, Distance = 10 },
            new() { Timestamp = timestamp, Latitude = 51, Longitude = -1, Altitude = null, Distance = 10 },
            new() { Timestamp = timestamp, Latitude = 51, Longitude = 181, Altitude = 5000, Distance = 10 },
            new() { Timestamp = timestamp, Latitude = 51, Longitude = -1, Altitude = 5000, Distance = 10 }
        ];

        var path = builder.Build(6, "ABC126", "TEST6", string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, points);

        Assert.HasCount(1, path.Points);
        Assert.AreEqual(timestamp, path.Points[0].Timestamp);
        Assert.AreEqual(51d, path.Points[0].Latitude);
        Assert.AreEqual(-1d, path.Points[0].Longitude);
        Assert.AreEqual(5000d, path.Points[0].AltitudeFeet);
    }

    /// <summary>
    /// Verifies an isolated impossible geographic excursion is removed without losing the surrounding path.
    /// </summary>
    [TestMethod]
    public void BuildRejectsIsolatedPositionSpike()
    {
        var builder = new FlightPathBuilder(null, null, new GeographicCalculator());
        var timestamp = new DateTime(2026, 8, 10, 10, 40, 0, DateTimeKind.Utc);
        FlightProfilePoint[] points =
        [
            new() { Timestamp = timestamp, Latitude = 51.66540m, Longitude = -0.82954m, Altitude = 20775, Distance = 16 },
            new() { Timestamp = timestamp.AddSeconds(1), Latitude = -46.01083m, Longitude = 98.18703m, Altitude = 20775, Distance = 7771 },
            new() { Timestamp = timestamp.AddSeconds(2), Latitude = 51.66165m, Longitude = -0.83296m, Altitude = 20800, Distance = 16 }
        ];

        var path = builder.Build(4, "407E82", "RUK9NE", string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, points);

        Assert.HasCount(2, path.Points);
        Assert.IsFalse(path.Points.Any(point => Math.Abs(point.Latitude - -46.01083) < 0.000001));
        Assert.IsLessThan(1000d, Math.Abs(path.Points[1].LocalXMetres));
        Assert.IsLessThan(1000d, Math.Abs(path.Points[1].LocalYMetres));
    }

    /// <summary>
    /// Verifies a one-record altitude excursion is removed while a sustained altitude step is retained.
    /// </summary>
    [TestMethod]
    public void BuildRejectsIsolatedAltitudeSpikeButRetainsSustainedStep()
    {
        var builder = new FlightPathBuilder(null, null, new GeographicCalculator());
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

        var path = builder.Build(5, "ABED10", "FDX5184", string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, points);

        Assert.HasCount(5, path.Points);
        Assert.IsFalse(path.Points.Any(point => Math.Abs(point.AltitudeFeet - 114500) < 0.001));
        Assert.AreEqual(3, path.Points.Count(point => Math.Abs(point.AltitudeFeet - 6500) < 0.001));
    }

    /// <summary>Verifies both path renderers receive no repeated, out-of-bounds, or terminal altitude spikes.</summary>
    [TestMethod]
    public void BuildRejectsAltitudeRunBoundsAndTerminalSpike()
    {
        var builder = new FlightPathBuilder(null, null, new GeographicCalculator());
        var timestamp = new DateTime(2026, 8, 12, 10, 0, 0, DateTimeKind.Utc);
        FlightProfilePoint[] points =
        [
            Point(timestamp, 0, 37000),
            Point(timestamp, 1, 60000),
            Point(timestamp, 10, 60000),
            Point(timestamp, 50, 60000),
            Point(timestamp, 51, 37000),
            Point(timestamp, 60, 70000),
            Point(timestamp, 61, 37000),
            Point(timestamp, 103, 48000)
        ];

        var path = builder.Build(7, "ABC127", "TEST7", string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, points);

        Assert.HasCount(3, path.Points);
        Assert.IsTrue(path.Points.All(point => Math.Abs(point.AltitudeFeet - 37000) < 0.001));
    }

    /// <summary>Verifies elapsed time permits a genuine path altitude change after missing receiver messages.</summary>
    [TestMethod]
    public void BuildRetainsPlausibleAltitudeChangeAfterLongGap()
    {
        var builder = new FlightPathBuilder(null, null, new GeographicCalculator());
        var timestamp = new DateTime(2026, 8, 12, 10, 0, 0, DateTimeKind.Utc);
        FlightProfilePoint[] points =
        [
            Point(timestamp, 0, 3300),
            Point(timestamp, 120, 14750),
            Point(timestamp, 121, 14750)
        ];

        var path = builder.Build(8, "ABC128", "TEST8", string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, points);

        Assert.HasCount(3, path.Points);
        Assert.AreEqual(2, path.SegmentCount);
        Assert.AreEqual(14750d, path.Points[^1].AltitudeFeet, 0.001);
    }

    /// <summary>Creates a nearby complete observation for spike-filter tests.</summary>
    private static FlightProfilePoint Point(DateTime timestamp, int seconds, decimal altitude) => new()
    {
        Timestamp = timestamp.AddSeconds(seconds),
        Latitude = 51m - seconds * 0.0001m,
        Longitude = -1m - seconds * 0.0001m,
        Altitude = altitude,
        Distance = 10
    };
}

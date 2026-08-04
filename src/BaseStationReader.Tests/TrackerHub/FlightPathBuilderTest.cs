using BaseStationReader.BusinessLogic.Tracking;
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
        var builder = new FlightPathBuilder(51.47, -0.454);
        var timestamp = new DateTime(2026, 7, 17, 12, 0, 0, DateTimeKind.Utc);
        FlightProfilePointDto[] points =
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
        var builder = new FlightPathBuilder(null, null);
        var timestamp = DateTime.UtcNow;
        var duplicate = new FlightProfilePointDto { Timestamp = timestamp, Latitude = 51m, Longitude = -1m, Altitude = 5000, Distance = 10 };
        FlightProfilePointDto[] points =
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
        var builder = new FlightPathBuilder(null, null);
        FlightProfilePointDto[] points =
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
}

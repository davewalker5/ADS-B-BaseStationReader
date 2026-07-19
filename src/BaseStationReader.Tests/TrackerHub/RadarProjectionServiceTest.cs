using BaseStationReader.Entities.Hub;
using BaseStationReader.TrackerHub.Services;

namespace BaseStationReader.Tests.TrackerHub;

[TestClass]
public class RadarProjectionServiceTest
{
    /// <summary>
    /// Verifies north and east bearings convert to the expected SVG axes.
    /// </summary>
    [TestMethod]
    public void ProjectConvertsBearingAndRangeToRadarCoordinates()
    {
        var service = new RadarProjectionService(0, 0);
        var north = Aircraft("NORTH1", 0.1m, 0m, 25);
        var east = Aircraft("EAST01", 0m, 0.1m, 25);

        // Half-range targets should sit halfway from the receiver along their compass axes.
        var northPoint = service.Project(north, 50);
        var eastPoint = service.Project(east, 50);

        Assert.IsNotNull(northPoint);
        Assert.IsNotNull(eastPoint);
        Assert.AreEqual(0d, northPoint.X, 0.001);
        Assert.AreEqual(-0.5d, northPoint.Y, 0.001);
        Assert.AreEqual(0.5d, eastPoint.X, 0.001);
        Assert.AreEqual(0d, eastPoint.Y, 0.001);
    }

    /// <summary>
    /// Verifies missing telemetry and invalid range do not create misleading radar targets.
    /// </summary>
    [TestMethod]
    public void ProjectRejectsIncompletePositionData()
    {
        var service = new RadarProjectionService(51.47, -0.45);
        var aircraft = Aircraft("EMPTY1", null, null, 10);

        // The radar must not place an incomplete target at the receiver origin.
        Assert.IsNull(service.Project(aircraft, 50));
        aircraft.Latitude = 51.5m;
        aircraft.Longitude = -0.4m;
        Assert.IsNull(service.Project(aircraft, 0));
    }

    /// <summary>
    /// Verifies stored polar trail values are consistently reprojected when radar range changes.
    /// </summary>
    [TestMethod]
    public void ProjectCoordinatesRescalesExistingTrailPoint()
    {
        var service = new RadarProjectionService(0, 0);

        // The same eastbound point should halve its screen radius when maximum range doubles.
        var shortRange = service.ProjectCoordinates(25, 90, 50);
        var longRange = service.ProjectCoordinates(25, 90, 100);

        Assert.IsNotNull(shortRange);
        Assert.IsNotNull(longRange);
        Assert.AreEqual(0.5d, shortRange.Value.X, 0.001);
        Assert.AreEqual(0.25d, longRange.Value.X, 0.001);
        Assert.AreEqual(0d, shortRange.Value.Y, 0.001);
        Assert.AreEqual(0d, longRange.Value.Y, 0.001);
    }

    /// <summary>
    /// Creates concise live telemetry for projection tests.
    /// </summary>
    /// <param name="address">ICAO-like identity.</param>
    /// <param name="latitude">Aircraft latitude.</param>
    /// <param name="longitude">Aircraft longitude.</param>
    /// <param name="distance">Receiver distance in nautical miles.</param>
    /// <returns>A live aircraft DTO.</returns>
    private static TrackedAircraftDto Aircraft(string address, decimal? latitude, decimal? longitude, double distance)
    {
        // Keep fixture values focused on coordinates used by the projection service.
        return new TrackedAircraftDto
        {
            Address = address,
            Callsign = address,
            Latitude = latitude,
            Longitude = longitude,
            Distance = distance,
            LastSeen = DateTime.UtcNow
        };
    }
}

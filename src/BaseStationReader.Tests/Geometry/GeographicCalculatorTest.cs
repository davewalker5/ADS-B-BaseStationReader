using BaseStationReader.BusinessLogic.Geometry;

namespace BaseStationReader.Tests.Geometry;

[TestClass]
public class GeographicCalculatorTest
{
    private readonly GeographicCalculator _calculator = new();

    /// <summary>
    /// Verifies finite coordinate bounds are applied consistently.
    /// </summary>
    [TestMethod]
    public void ValidateCoordinateBoundsTest()
    {
        // Boundary coordinates are valid, while non-finite and out-of-range values are rejected.
        Assert.IsTrue(_calculator.IsValidCoordinate(90, 180));
        Assert.IsTrue(_calculator.IsValidCoordinate(-90, -180));
        Assert.IsFalse(_calculator.IsValidCoordinate(90.001, 0));
        Assert.IsFalse(_calculator.IsValidCoordinate(0, -180.001));
        Assert.IsFalse(_calculator.IsValidCoordinate(double.NaN, 0));
    }

    /// <summary>
    /// Verifies initial bearings use conventional clockwise compass degrees.
    /// </summary>
    [TestMethod]
    public void CalculateCardinalBearingsTest()
    {
        // Simple equatorial destinations provide unambiguous north and east expectations.
        Assert.AreEqual(0d, _calculator.CalculateInitialBearing(0, 0, 1, 0), 0.0001);
        Assert.AreEqual(90d, _calculator.CalculateInitialBearing(0, 0, 0, 1), 0.0001);
    }

    /// <summary>
    /// Verifies great-circle distance and interpolation share the same spherical model.
    /// </summary>
    [TestMethod]
    public void CalculateAndInterpolateGreatCircleTest()
    {
        // A quarter of the equator has a central angle of pi over two and a midpoint at 45 degrees east.
        Assert.AreEqual(Math.PI / 2d, _calculator.CalculateAngularDistance(0, 0, 0, 90), 0.0001);
        var midpoint = _calculator.InterpolateGreatCircle(0, 0, 0, 90, 0.5);
        Assert.AreEqual(0d, midpoint.Latitude, 0.0001);
        Assert.AreEqual(45d, midpoint.Longitude, 0.0001);
    }

    /// <summary>
    /// Verifies linear distance uses the shared mean Earth radius.
    /// </summary>
    [TestMethod]
    public void CalculateDistanceMetresTest()
    {
        // One degree along the equator is approximately 111.195 kilometres on the mean-radius sphere.
        var distance = _calculator.CalculateDistanceMetres(0, 0, 0, 1);
        Assert.AreEqual(111194.9d, distance, 0.2d);
    }

    /// <summary>
    /// Verifies destination calculations use the same distance and bearing model.
    /// </summary>
    [TestMethod]
    public void CalculateDestinationPointTest()
    {
        // Travelling the one-degree equatorial distance due east should arrive at one degree longitude.
        var destination = _calculator.CalculateDestinationPoint(0, 0, 90, 111194.9d);
        Assert.AreEqual(0d, destination.Latitude, 0.0001);
        Assert.AreEqual(1d, destination.Longitude, 0.0001);
    }

    /// <summary>
    /// Verifies antimeridian interpolation follows the short route.
    /// </summary>
    [TestMethod]
    public void InterpolateAcrossAntimeridianTest()
    {
        // The midpoint between 170 east and 170 west should lie on the antimeridian, not Greenwich.
        var midpoint = _calculator.InterpolateGreatCircle(0, 170, 0, -170, 0.5);
        Assert.AreEqual(180d, Math.Abs(midpoint.Longitude), 0.0001);
    }

    /// <summary>
    /// Verifies local projection produces east and north metre offsets from its origin.
    /// </summary>
    [TestMethod]
    public void ProjectToLocalMetresTest()
    {
        // A small north-east movement must produce positive offsets on both local axes.
        var projected = _calculator.ProjectToLocalMetres(51, -1, 51.01, -0.99);
        Assert.IsGreaterThan(0d, projected.EastMetres);
        Assert.IsGreaterThan(0d, projected.NorthMetres);
    }
}

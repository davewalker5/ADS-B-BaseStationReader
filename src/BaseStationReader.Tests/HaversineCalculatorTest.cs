using BaseStationReader.Logic.Maths;

namespace BaseStationReader.Tests
{
    [TestClass]
    public class HaversineCalculatorTest
    {
        private const double LHR_LATITUDE = 51.47138888;
        private const double LHR_LONGITUDE = -0.45277777;

        private const double CDG_LATITUDE = 49.009724;
        private const double CDG_LONGITUDE = 2.547778;

        [TestMethod]
        public void HaversineDistanceTest()
        {
            var metres = new HaversineCalculator().CalculateDistance(LHR_LATITUDE, LHR_LONGITUDE, CDG_LATITUDE, CDG_LONGITUDE);
            var rounded = Math.Round(metres, MidpointRounding.AwayFromZero);
            Assert.AreEqual(347392, rounded);
        }

        [TestMethod]
        public void HaversineDistanceFromReferencePositionTest()
        {
            var calculator = new HaversineCalculator
            {
                ReferenceLatitude = LHR_LATITUDE,
                ReferenceLongitude = LHR_LONGITUDE
            };
            var metres = calculator.CalculateDistance(CDG_LATITUDE, CDG_LONGITUDE);
            var rounded = Math.Round(metres, MidpointRounding.AwayFromZero);
            Assert.AreEqual(347392, rounded);
        }

        [TestMethod]
        public void NauticalMilesTest()
        {
            var calculator = new HaversineCalculator();
            var metres = calculator.CalculateDistance(LHR_LATITUDE, LHR_LONGITUDE, CDG_LATITUDE, CDG_LONGITUDE);
            var nm = calculator.MetresToNauticalMiles(metres);
            var rounded = Math.Round(nm, MidpointRounding.AwayFromZero);
            Assert.AreEqual(188, rounded);
        }
    }
}

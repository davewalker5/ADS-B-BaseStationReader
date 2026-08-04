using BaseStationReader.BusinessLogic.Geometry;

namespace BaseStationReader.Tests.Tracking
{
    [TestClass]
    public class ReceiverDistanceCalculatorTest
    {
        private const double LHR_LATITUDE = 51.47138888;
        private const double LHR_LONGITUDE = -0.45277777;

        private const double CDG_LATITUDE = 49.009724;
        private const double CDG_LONGITUDE = 2.547778;

        [TestMethod]
        public void HaversineDistanceTest()
        {
            var metres = new ReceiverDistanceCalculator(new GeographicCalculator())
                .CalculateDistance(LHR_LATITUDE, LHR_LONGITUDE, CDG_LATITUDE, CDG_LONGITUDE);
            var rounded = Math.Round(metres, MidpointRounding.AwayFromZero);
            Assert.AreEqual(347011, rounded);
        }

        [TestMethod]
        public void HaversineDistanceFromReferencePositionTest()
        {
            var calculator = new ReceiverDistanceCalculator(new GeographicCalculator())
            {
                ReferenceLatitude = LHR_LATITUDE,
                ReferenceLongitude = LHR_LONGITUDE
            };
            var metres = calculator.CalculateDistance(CDG_LATITUDE, CDG_LONGITUDE);
            var rounded = Math.Round(metres, MidpointRounding.AwayFromZero);
            Assert.AreEqual(347011, rounded);
        }

        [TestMethod]
        public void NauticalMilesTest()
        {
            var calculator = new ReceiverDistanceCalculator(new GeographicCalculator());
            var metres = calculator.CalculateDistance(LHR_LATITUDE, LHR_LONGITUDE, CDG_LATITUDE, CDG_LONGITUDE);
            var nm = calculator.MetresToNauticalMiles(metres);
            var rounded = Math.Round(nm, MidpointRounding.AwayFromZero);
            Assert.AreEqual(187, rounded);
        }
    }
}

using System.Text.RegularExpressions;
using BaseStationReader.BusinessLogic.Simulator;
using BaseStationReader.Entities.Tracking;
using BaseStationReader.Interfaces.Logging;
using BaseStationReader.Tests.Mocks;

namespace BaseStationReader.Tests.Simulator
{
    [TestClass]
    public class AircraftGeneratorTest : SimulatorTestBase
    {
        private ITrackerLogger _logger = new MockFileLogger();

        [TestMethod]
        public void GenerateNewAircraftTest()
        {
            var generator = new AircraftGenerator(_logger, _settings, null);
            var aircraft = generator.Generate([]);

            Assert.IsNotNull(aircraft);
            Assert.IsGreaterThanOrEqualTo(_settings.MinimumAircraftLifespan, aircraft.Lifespan);
            Assert.IsLessThanOrEqualTo(_settings.MaximumAircraftLifespan, aircraft.Lifespan);
            Assert.IsTrue(Regex.Match(aircraft.Address, @"^[A-Za-z0-9]{6}$").Success);
            Assert.IsTrue(Regex.Match(aircraft.Callsign, @"^[A-Za-z0-9]{7}$").Success);
            Assert.IsGreaterThanOrEqualTo(0, aircraft.Track.Value);
            Assert.IsLessThanOrEqualTo(360, aircraft.Track.Value);
            Assert.AreEqual(4, aircraft.Squawk?.Length);
            Assert.IsNotNull(aircraft.Track);
            Assert.IsNotNull(aircraft.Latitude);
            Assert.IsNotNull(aircraft.Longitude);

            switch (aircraft.Behaviour)
            {
                case AircraftBehaviour.Climbing:
                    Assert.IsGreaterThanOrEqualTo(_settings.MinimumTakeOffSpeed, aircraft.GroundSpeed.Value);
                    Assert.IsLessThanOrEqualTo(_settings.MaximumTakeOffSpeed, aircraft.GroundSpeed.Value);
                    Assert.IsGreaterThanOrEqualTo(_settings.MinimumClimbRate, aircraft.VerticalRate.Value);
                    Assert.IsLessThanOrEqualTo(_settings.MaximumClimbRate, aircraft.VerticalRate.Value);
                    Assert.AreEqual(0, aircraft.Altitude.Value);
                    break;
                case AircraftBehaviour.Descending:
                    Assert.IsGreaterThanOrEqualTo(_settings.MinimumApproachSpeed, aircraft.GroundSpeed.Value);
                    Assert.IsLessThanOrEqualTo(_settings.MaximumApproachSpeed, aircraft.GroundSpeed.Value);
                    Assert.IsGreaterThanOrEqualTo(_settings.MinimumDescentRate, Math.Abs(aircraft.VerticalRate.Value));
                    Assert.IsLessThanOrEqualTo(_settings.MaximumDescentRate, Math.Abs(aircraft.VerticalRate.Value));

                    var expectedAltitude = Math.Abs(aircraft.VerticalRate.Value) * aircraft.Lifespan / 1000M;
                    Assert.AreEqual(expectedAltitude, aircraft.Altitude);
                    break;
                default:
                    Assert.IsGreaterThanOrEqualTo(_settings.MinimumCruisingSpeed, aircraft.GroundSpeed.Value);
                    Assert.IsLessThanOrEqualTo(_settings.MaximumCruisingSpeed, aircraft.GroundSpeed.Value);
                    Assert.AreEqual(0, aircraft.VerticalRate);
                    Assert.IsGreaterThanOrEqualTo(_settings.MinimumAltitude, aircraft.Altitude.Value);
                    Assert.IsLessThanOrEqualTo(_settings.MaximumAltitude, aircraft.Altitude.Value);
                    break;
            }

        }

        [TestMethod]
        public void CuratedAddressListTest()
        {
            var curated = AircraftGenerator.CuratedAddressList(["ABC123", "This is not valid", "456DEF"]);
            Assert.HasCount(2, curated);
            Assert.Contains("ABC123", curated);
            Assert.Contains("456DEF", curated);
        }

        [TestMethod]
        public void SelectAddressWithNoExistingAddressesTest()
        {
            var generator = new AircraftGenerator(_logger, _settings, ["ABC123", "456DEF"]);

            var address = generator.SelectAddress(null);
            Assert.AreEqual("ABC123", address);

            address = generator.SelectAddress(null);
            Assert.AreEqual("456DEF", address);
        }

        [TestMethod]
        public void SelectAddressWithExistingAddressesTest()
        {
            var generator = new AircraftGenerator(_logger, _settings, ["ABC123", "456DEF"]);

            var address = generator.SelectAddress(["ABC123"]);
            Assert.AreEqual("456DEF", address);

            address = generator.SelectAddress(["ABC123"]);
            Assert.AreEqual("456DEF", address);
        }

        [TestMethod]
        public void SelectAddressWithNoAddressListTest()
        {
            var generator = new AircraftGenerator(_logger, _settings, null);

            var address = generator.SelectAddress(null);
            Assert.IsNull(address);
        }
    }
}

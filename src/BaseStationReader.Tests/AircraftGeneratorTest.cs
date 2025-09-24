using BaseStationReader.BusinessLogic.Simulator;
using BaseStationReader.Entities.Config;
using BaseStationReader.Tests.Mocks;

namespace BaseStationReader.Tests
{
    [TestClass]
    public class AircraftGeneratorTest : SimulatorTestBase
    {

        [TestMethod]
        public void GenerateNewAircraftTest()
        {
            var logger = new MockFileLogger();
            var generator = new AircraftGenerator(logger, _settings, null);
            var aircraft = generator.Generate(new List<string>());

            Assert.IsNotNull(aircraft);
            Assert.AreEqual(6, aircraft.Address.Length);
            Assert.AreEqual(7, aircraft.Callsign!.Length);
            Assert.AreEqual(4, aircraft.Squawk?.Length);
            Assert.IsNotNull(aircraft.FirstSeen);
            Assert.IsNotNull(aircraft.LastSeen);
            Assert.IsNotNull(aircraft.Track);
            Assert.IsNotNull(aircraft.GroundSpeed);
            Assert.IsNotNull(aircraft.VerticalRate);
            Assert.IsNotNull(aircraft.Altitude);
            Assert.IsNotNull(aircraft.Latitude);
            Assert.IsNotNull(aircraft.Longitude);
        }
    }
}

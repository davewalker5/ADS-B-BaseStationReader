using BaseStationReader.Logic.Simulator;
using BaseStationReader.Tests.Mocks;

namespace BaseStationReader.Tests
{
    [TestClass]
    public class AircraftGeneratorTest
    {
        [TestMethod]
        public void GenerateNewAircraftTest()
        {
            var logger = new MockFileLogger();
            var generator = new AircraftGenerator(logger);
            var aircraft = generator.Generate(new List<string>());

            Assert.IsNotNull(aircraft);
            Assert.AreEqual(6, aircraft.Address.Length);
            Assert.AreEqual(4, aircraft.Squawk?.Length);
            Assert.IsNotNull(aircraft.FirstSeen);
            Assert.IsNotNull(aircraft.LastSeen);
        }
    }
}

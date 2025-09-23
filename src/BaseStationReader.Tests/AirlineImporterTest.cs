using BaseStationReader.BusinessLogic.Database;
using BaseStationReader.BusinessLogic.Logging;
using BaseStationReader.Data;
using BaseStationReader.Entities.Interfaces;
using BaseStationReader.Tests.Mocks;

namespace BaseStationReader.Tests
{
    [TestClass]
    public class AirlineImporterTest
    {
        private IAirlineManager _airlineManager;
        private IAirlineImporter _importer;

        [TestInitialize]
        public void Initialise()
        {
            var context = BaseStationReaderDbContextFactory.CreateInMemoryDbContext();
            var logger = new MockFileLogger();
            _airlineManager = new AirlineManager(context);
            _importer = new AirlineImporter(_airlineManager, logger);
        }

        [TestMethod]
        public async Task ImportTest()
        {
            await _importer.Import("airlines.csv");
            var airlines = await _airlineManager.ListAsync(x => true);

            Assert.IsNotNull(airlines);
            Assert.HasCount(1, airlines);
            Assert.IsGreaterThan(0, airlines[0].Id);
            Assert.AreEqual("BAW", airlines[0].ICAO);
            Assert.AreEqual("BA", airlines[0].IATA);
            Assert.AreEqual("British Airways", airlines[0].Name);
        }

        [TestMethod]
        public async Task ImportEmptyFileTest()
        {
            await _importer.Import("empty_airlines.csv");
            var airlines = await _airlineManager.ListAsync(x => true);

            Assert.IsNotNull(airlines);
            Assert.HasCount(0, airlines);
        }

        [TestMethod]
        public async Task ImportMissingFileTest()
        {
            await _importer.Import("missing.csv");
            var airlines = await _airlineManager.ListAsync(x => true);

            Assert.IsNotNull(airlines);
            Assert.HasCount(0, airlines);
        }
    }
}
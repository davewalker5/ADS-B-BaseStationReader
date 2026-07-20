using BaseStationReader.BusinessLogic.Database;
using BaseStationReader.BusinessLogic.Logging;
using BaseStationReader.Data;
using BaseStationReader.Interfaces.Database;
using BaseStationReader.Interfaces.DataExchange;
using BaseStationReader.Tests.Mocks;

namespace BaseStationReader.Tests.DataExchange
{
    [TestClass]
    public class AirportImporterTest
    {
        private IDatabaseManagementFactory _factory;
        private IAirportImporter _importer;

        /// <summary>
        /// Create an airport importer backed by a fresh in-memory database.
        /// </summary>
        [TestInitialize]
        public void Initialise()
        {
            var context = BaseStationReaderDbContextFactory.CreateInMemoryDbContext();
            _factory = new DatabaseManagementFactory(new MockFileLogger(), context, 0, 0);
            _importer = new AirportImporter(_factory);
        }

        /// <summary>
        /// Verify every field in the airport CSV format is imported.
        /// </summary>
        [TestMethod]
        public async Task ImportTestAsync()
        {
            await _importer.ImportAsync("airports.csv");
            var airports = await _factory.AirportManager.ListAsync(x => true);

            Assert.HasCount(1, airports);
            Assert.AreEqual("ALC", airports[0].IATA);
            Assert.AreEqual("LEAL", airports[0].ICAO);
            Assert.AreEqual(38.2822, airports[0].Latitude);
            Assert.AreEqual(-0.55816, airports[0].Longitude);
            Assert.AreEqual(804.5, airports[0].Distance);
            Assert.AreEqual("Alicante International Airport", airports[0].Name);
        }

        /// <summary>
        /// Verify a header-only airport file imports no records.
        /// </summary>
        [TestMethod]
        public async Task ImportEmptyFileTestAsync()
        {
            await _importer.ImportAsync("empty_airports.csv");
            Assert.IsEmpty(await _factory.AirportManager.ListAsync(x => true));
        }

        /// <summary>
        /// Verify a missing airport file imports no records.
        /// </summary>
        [TestMethod]
        public async Task ImportMissingFileTestAsync()
        {
            await _importer.ImportAsync("missing-airports.csv");
            Assert.IsEmpty(await _factory.AirportManager.ListAsync(x => true));
        }
    }
}

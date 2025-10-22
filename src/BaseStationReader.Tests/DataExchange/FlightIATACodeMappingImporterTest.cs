using BaseStationReader.BusinessLogic.Database;
using BaseStationReader.BusinessLogic.Logging;
using BaseStationReader.Data;
using BaseStationReader.Interfaces.DataExchange;
using BaseStationReader.Interfaces.Database;
using BaseStationReader.Tests.Mocks;
using BaseStationReader.Entities.Api;

namespace BaseStationReader.Tests.DataExchange
{
    [TestClass]
    public class FlightIATACodeMappingImporterTest
    {
        private IDatabaseManagementFactory _factory;
        private IFlightIATACodeMappingImporter _importer;

        [TestInitialize]
        public void Initialise()
        {
            var context = BaseStationReaderDbContextFactory.CreateInMemoryDbContext();
            var logger = new MockFileLogger();
            _factory = new DatabaseManagementFactory(logger, context, 0, 0);
            _importer = new FlightIATACodeMappingImporter(_factory);
        }

        [TestMethod]
        public async Task ImportTestAsync()
        {
            await _importer.ImportAsync("flight_number_mappings.csv");
            var mappings = await _factory.FlightIATACodeMappingManager.ListAsync(x => true);

            Assert.IsNotNull(mappings);
            Assert.HasCount(1, mappings);
            Assert.IsGreaterThan(0, mappings[0].Id);
            Assert.AreEqual("BAW2038", mappings[0].Callsign);
            Assert.AreEqual("BA2038", mappings[0].FlightIATA);
            Assert.AreEqual("BA", mappings[0].AirlineIATA);
            Assert.AreEqual("BAW", mappings[0].AirlineICAO);
            Assert.AreEqual("British Airways", mappings[0].AirlineName);
            Assert.AreEqual("MCO", mappings[0].AirportIATA);
            Assert.AreEqual("KMCO", mappings[0].AirportICAO);
            Assert.AreEqual("Orlando", mappings[0].AirportName);
            Assert.AreEqual(AirportType.Arrival, mappings[0].AirportType);
            Assert.AreEqual("2025-10-12-LGW.json", mappings[0].FileName);
        }

        [TestMethod]
        public async Task ImportEmptyFileTestAsync()
        {
            await _importer.ImportAsync("empty_mappings.csv");
            var airlines = await _factory.FlightIATACodeMappingManager.ListAsync(x => true);

            Assert.IsNotNull(airlines);
            Assert.HasCount(0, airlines);
        }

        [TestMethod]
        public async Task ImportMissingFileTestAsync()
        {
            await _importer.ImportAsync("missing.csv");
            var airlines = await _factory.FlightIATACodeMappingManager.ListAsync(x => true);

            Assert.IsNotNull(airlines);
            Assert.HasCount(0, airlines);
        }
    }
}
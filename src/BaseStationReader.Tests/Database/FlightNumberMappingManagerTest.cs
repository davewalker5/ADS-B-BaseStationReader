using BaseStationReader.BusinessLogic.Database;
using BaseStationReader.Data;
using BaseStationReader.Entities.Api;
using BaseStationReader.Interfaces.Database;
using BaseStationReader.Tests.Mocks;

namespace BaseStationReader.Tests.Database
{
    [TestClass]
    public class FlightNumberMappingManagerTest
    {
        private const string AirlineIATA = "EI";
        private const string AirlineICAO = "EIN";
        private const string AirlineName = "Aer Lingus";
        private const string FlightIATA = "EI527";
        private const string AirportICAO = "EGLL";
        private const string AirportIATA = "LHR";
        private const string AirportName = "London Heathrow";
        private const string Callsign = "EIN5KM";

        private IDatabaseManagementFactory _factory;

        [TestInitialize]
        public async Task InitialiseAsync()
        {
            // Create a database management factory
            var logger = new MockFileLogger();
            var context = BaseStationReaderDbContextFactory.CreateInMemoryDbContext();
            _factory = new DatabaseManagementFactory(logger, context, 0, 0);

            // Add a callsign/flight number mapping
            await _factory.FlightNumberMappingManager.AddAsync(
                AirlineICAO,
                AirlineIATA,
                AirlineName,
                AirportICAO,
                AirportIATA,
                AirportName,
                AirportType.Unknown,
                FlightIATA,
                Callsign,
                "");
        }

        [TestMethod]
        public async Task GetAsyncTestAsync()
        {
            var mapping = await _factory.FlightNumberMappingManager.GetAsync(x => x.Callsign == Callsign);

            Assert.IsNotNull(mapping);
            Assert.AreEqual(AirlineICAO, mapping.AirlineICAO);
            Assert.AreEqual(AirlineIATA, mapping.AirlineIATA);
            Assert.AreEqual(AirlineName, mapping.AirlineName);
            Assert.AreEqual(AirportICAO, mapping.AirportICAO);
            Assert.AreEqual(AirportIATA, mapping.AirportIATA);
            Assert.AreEqual(AirportName, mapping.AirportName);
            Assert.AreEqual(AirportType.Unknown, mapping.AirportType);
            Assert.AreEqual(Callsign, mapping.Callsign);
            Assert.AreEqual(FlightIATA, mapping.FlightIATA);
            Assert.IsEmpty(mapping.FileName);
        }

        [TestMethod]
        public async Task ListAsyncTestAsync()
        {
            var mappings = await _factory.FlightNumberMappingManager.ListAsync(x => true);

            Assert.IsNotNull(mappings);
            Assert.HasCount(1, mappings);
            Assert.AreEqual(AirlineICAO, mappings[0].AirlineICAO);
            Assert.AreEqual(AirlineIATA, mappings[0].AirlineIATA);
            Assert.AreEqual(AirlineName, mappings[0].AirlineName);
            Assert.AreEqual(AirportICAO, mappings[0].AirportICAO);
            Assert.AreEqual(AirportIATA, mappings[0].AirportIATA);
            Assert.AreEqual(AirportName, mappings[0].AirportName);
            Assert.AreEqual(AirportType.Unknown, mappings[0].AirportType);
            Assert.AreEqual(Callsign, mappings[0].Callsign);
            Assert.AreEqual(FlightIATA, mappings[0].FlightIATA);
            Assert.IsEmpty(mappings[0].FileName);
        }

        [TestMethod]
        public async Task UpdateTestAsync()
        {
            var updatedAirlineICAO = AirlineICAO.Reverse().ToString();
            var updatedAirlineIATA = AirlineIATA.Reverse().ToString();
            var updatedAirlineName = AirlineName.Reverse().ToString();
            var updatedAirportICAO = AirportICAO.Reverse().ToString();
            var updatedAirportIATA = AirportIATA.Reverse().ToString();
            var updatedAirportName = AirportName.Reverse().ToString();
            var updatedFlightIATA = FlightIATA.Reverse().ToString();
            var updatedFileName = "Some File.json";

            var mapping = await _factory.FlightNumberMappingManager.AddAsync(
                updatedAirlineICAO,
                updatedAirlineIATA,
                updatedAirlineName,
                updatedAirportICAO,
                updatedAirportIATA,
                updatedAirportName,
                AirportType.Departure,
                updatedFlightIATA,
                Callsign,
                updatedFileName);

            Assert.IsNotNull(mapping);
            Assert.AreEqual(updatedAirlineICAO, mapping.AirlineICAO);
            Assert.AreEqual(updatedAirlineIATA, mapping.AirlineIATA);
            Assert.AreEqual(updatedAirlineName, mapping.AirlineName);
            Assert.AreEqual(updatedAirportICAO, mapping.AirportICAO);
            Assert.AreEqual(updatedAirportIATA, mapping.AirportIATA);
            Assert.AreEqual(updatedAirportName, mapping.AirportName);
            Assert.AreEqual(AirportType.Departure, mapping.AirportType);
            Assert.AreEqual(Callsign, mapping.Callsign);
            Assert.AreEqual(updatedFlightIATA, mapping.FlightIATA);
            Assert.AreEqual(updatedFileName, mapping.FileName);
        }
    }
}
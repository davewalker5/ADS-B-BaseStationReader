using BaseStationReader.Data;
using BaseStationReader.BusinessLogic.Database;
using BaseStationReader.Entities.Api;
using BaseStationReader.Interfaces.Database;

namespace BaseStationReader.Tests.Database
{
    [TestClass]
    public class FlightManagerTest
    {
        private const string FlightIATA = "BA185";
        private const string FlightICAO = "BAW185";
        private const string Embarkation = "LHR";
        private const string Destination = "EWR";
        private const string AirlineIATA = "BA";
        private const string AirlineICAO = "BAW";
        private const string AirlineName = "British Airways";

        private IFlightManager _manager = null;
        private Airline _airline;

        [TestInitialize]
        public async Task InitialiseAsync()
        {
            // Create a context and a flight management class to test
            BaseStationReaderDbContext context = BaseStationReaderDbContextFactory.CreateInMemoryDbContext();
            _manager = new FlightManager(context);

            // Set up n airline and a flight
            _airline = await new AirlineManager(context).AddAsync(AirlineIATA, AirlineICAO, AirlineName);
            _ = await _manager.AddAsync(FlightIATA, FlightICAO, Embarkation, Destination, _airline.Id);
        }

        [TestMethod]
        public async Task AddDuplicateTestAsync()
        {
            await  _manager.AddAsync(FlightIATA, FlightICAO, Embarkation, Destination, _airline.Id);
            var models = await _manager.ListAsync(x => true);
            Assert.HasCount(1, models);
        }

        [TestMethod]
        public async Task AddAndGetTestAsync()
        {
            var flight = await _manager.GetAsync(a => a.IATA == FlightIATA);
            Assert.IsNotNull(flight);
            Assert.IsGreaterThan(0, flight.Id);
            Assert.AreEqual(FlightIATA, flight.IATA);
            Assert.AreEqual(FlightICAO, flight.ICAO);
            Assert.AreEqual(Embarkation, flight.Embarkation);
            Assert.AreEqual(Destination, flight.Destination);
            Assert.AreEqual(AirlineName, flight.Airline.Name);
        }

        [TestMethod]
        public async Task GetMissingTestAsync()
        {
            var flight = await _manager.GetAsync(a => a.IATA == "Missing");
            Assert.IsNull(flight);
        }

        [TestMethod]
        public async Task ListAllTestAsync()
        {
            var flights = await _manager.ListAsync(x => true);
            Assert.IsNotNull(flights);
            Assert.HasCount(1, flights);
            Assert.IsGreaterThan(0, flights[0].Id);
            Assert.AreEqual(FlightIATA, flights[0].IATA);
            Assert.AreEqual(FlightICAO, flights[0].ICAO);
            Assert.AreEqual(Embarkation, flights[0].Embarkation);
            Assert.AreEqual(Destination, flights[0].Destination);
            Assert.AreEqual(AirlineName, flights[0].Airline.Name);
        }

        [TestMethod]
        public async Task ListMissingTestAsync()
        {
            var flights = await _manager.ListAsync(x => x.IATA == "Missing");
            Assert.IsEmpty(flights);
        }
    }
}


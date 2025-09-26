using BaseStationReader.Data;
using BaseStationReader.Entities.Interfaces;
using BaseStationReader.BusinessLogic.Database;
using BaseStationReader.Entities.Lookup;

namespace BaseStationReader.Tests.Database
{
    [TestClass]
    public class SightingManagerTest
    {
        private readonly string Address = new Random().Next(0, 16777215).ToString("X6");
        private const string Manufacturer = "Airbus";
        private const string ModelIATA = "332";
        private const string ModelICAO = "A332";
        private const string ModelName = "A330-200";
        private const string Registration = "G-ABCD";
        private const int Manufactured = 2014;
        private const string FlightNumber = "185";
        private const string FlightIATA = "BA185";
        private const string FlightICAO = "BAW185";
        private const string Embarkation = "LHR";
        private const string Destination = "EWR";
        private const string AirlineIATA = "BA";
        private const string AirlineICAO = "BAW";
        private const string AirlineName = "British Airways";

        private ISightingManager _manager = null;
        private Aircraft _aircraft;
        private Flight _flight;

        [TestInitialize]
        public async Task Initialise()
        {
            // Create a context and a sighting management class to test
            BaseStationReaderDbContext context = BaseStationReaderDbContextFactory.CreateInMemoryDbContext();
            _manager = new SightingManager(context);

            // Set up a manufacturer, an aircraft model and an aircraft
            int age = DateTime.Now.Year - Manufactured;
            var manufacturer = await new ManufacturerManager(context).AddAsync(Manufacturer);
            var model = await new ModelManager(context).AddAsync(ModelIATA, ModelICAO, ModelName, manufacturer.Id);
            _aircraft = await new AircraftManager(context).AddAsync(Address, Registration, Manufactured, age, model.Id);

            // Set up an airline and a flight
            var airline = await new AirlineManager(context).AddAsync(AirlineIATA, AirlineICAO, AirlineName);
            _flight = await new FlightManager(context).AddAsync(FlightIATA, FlightICAO, FlightNumber, Embarkation, Destination, airline.Id);
        }

        [TestMethod]
        public async Task AddTest()
        {
            var timestamp = DateTime.Today;
            var sighting = await _manager.AddAsync(_aircraft.Id, _flight.Id, timestamp);
            Assert.AreEqual(_aircraft.Id, sighting.AircraftId);
            Assert.AreEqual(_flight.Id, sighting.FlightId);
            Assert.AreEqual(timestamp, sighting.Timestamp);
        }

        [TestMethod]
        public async Task GetAsyncTest()
        {
            var timestamp = DateTime.Today;
            var sighting = await _manager.AddAsync(_aircraft.Id, _flight.Id, timestamp);
            var retrieved = await _manager.GetAsync(x => x.Id == sighting.Id);
            Assert.AreEqual(_aircraft.Id, retrieved.AircraftId);
            Assert.AreEqual(_flight.Id, retrieved.FlightId);
            Assert.AreEqual(timestamp, retrieved.Timestamp);
        }

        [TestMethod]
        public async Task ListAsyncTest()
        {
            var timestamp = DateTime.Today;
            _ = await _manager.AddAsync(_aircraft.Id, _flight.Id, timestamp);
            var sightings = await _manager.ListAsync(x => true);
            Assert.HasCount(1, sightings);
            Assert.AreEqual(_aircraft.Id, sightings[0].AircraftId);
            Assert.AreEqual(_flight.Id, sightings[0].FlightId);
            Assert.AreEqual(timestamp, sightings[0].Timestamp);
        }
    }
}


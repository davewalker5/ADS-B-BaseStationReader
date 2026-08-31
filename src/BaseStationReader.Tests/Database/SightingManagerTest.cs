using BaseStationReader.Data;
using BaseStationReader.BusinessLogic.Database;
using BaseStationReader.Entities.Api;
using BaseStationReader.Interfaces.Database;
using BaseStationReader.Entities.Tracking;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

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
        private const string FlightIATA = "BA185";
        private const string FlightICAO = "BAW185";
        private const string Embarkation = "LHR";
        private const string Destination = "EWR";
        private const string AirlineIATA = "BA";
        private const string AirlineICAO = "BAW";
        private const string AirlineName = "British Airways";

        private ISightingManager _manager = null;
        private Aircraft _aircraft;
        private Airline _airline;
        private Flight _flight;
        private BaseStationReaderDbContext _context;
        private SqliteConnection _connection;

        [TestInitialize]
        public async Task InitialiseAsync()
        {
            // Create a context and a sighting management class to test
            _connection = new SqliteConnection("Data Source=:memory:");
            await _connection.OpenAsync();
            var options = new DbContextOptionsBuilder<BaseStationReaderDbContext>()
                .UseSqlite(_connection)
                .Options;
            _context = new BaseStationReaderDbContext(options);
            await _context.Database.MigrateAsync();
            _manager = new SightingManager(_context);

            // Set up a manufacturer, an aircraft model and an aircraft
            int age = DateTime.Now.Year - Manufactured;
            var manufacturer = await new ManufacturerManager(_context).AddAsync(Manufacturer);
            var model = await new ModelManager(_context).AddAsync(ModelIATA, ModelICAO, ModelName, manufacturer.Id);
            _aircraft = await new AircraftManager(_context).AddAsync(Address, Registration, Manufactured, age, model.Id);

            // Set up an airline and a flight
            _airline = await new AirlineManager(_context).AddAsync(AirlineIATA, AirlineICAO, AirlineName);
            var airportManager = new AirportManager(_context);
            var origin = await airportManager.AddAsync(new Airport
            {
                IATA = Embarkation, ICAO = "EGLL", Name = "London Heathrow", ProvenanceId = _airline.ProvenanceId
            });
            var destination = await airportManager.AddAsync(new Airport
            {
                IATA = Destination, ICAO = "KEWR", Name = "Newark", ProvenanceId = _airline.ProvenanceId
            });
            _flight = await new FlightManager(_context).AddAsync(
                FlightIATA, FlightICAO, FlightICAO, _airline.Id, origin.Id, destination.Id);

            await _context.TrackedAircraft.AddAsync(new TrackedAircraft
            {
                Address = Address,
                Callsign = FlightICAO,
                FirstSeen = DateTime.Today,
                LastSeen = DateTime.Today.AddMinutes(1),
                Status = TrackingStatus.Locked
            });
            await _context.SaveChangesAsync();
        }

        [TestMethod]
        public async Task GetTestAsync()
        {
            var retrieved = await _manager.GetAsync(x => x.AircraftId == _aircraft.Id);
            Assert.AreEqual(_aircraft.Id, retrieved.AircraftId);
            Assert.AreEqual(_flight.Id, retrieved.FlightId);
            Assert.AreEqual(_airline.Id, retrieved.AirlineId);
            Assert.AreEqual(DateTime.Today, retrieved.Timestamp);
        }

        [TestMethod]
        public async Task ListTestAsync()
        {
            var sightings = await _manager.ListAsync(x => true);
            Assert.HasCount(1, sightings);
            Assert.AreEqual(_aircraft.Id, sightings[0].AircraftId);
            Assert.AreEqual(_flight.Id, sightings[0].FlightId);
            Assert.AreEqual(_airline.Id, sightings[0].AirlineId);
            Assert.AreEqual(DateTime.Today, sightings[0].Timestamp);
        }

        [TestMethod]
        public async Task UnknownFlightResolvesAirlineFromCallsignTestAsync()
        {
            var tracked = new TrackedAircraft
            {
                Address = Address,
                Callsign = "BAW999X",
                FirstSeen = DateTime.Today.AddHours(1),
                LastSeen = DateTime.Today.AddHours(2),
                Status = TrackingStatus.Locked
            };
            await _context.TrackedAircraft.AddAsync(tracked);
            await _context.SaveChangesAsync();

            var retrieved = await _manager.GetAsync(x => x.Id == tracked.Id);

            Assert.IsNotNull(retrieved);
            Assert.IsNull(retrieved.FlightId);
            Assert.IsNull(retrieved.Flight);
            Assert.AreEqual(_airline.Id, retrieved.AirlineId);
            Assert.AreEqual(AirlineName, retrieved.Airline.Name);
        }

        [TestCleanup]
        public async Task CleanupAsync()
        {
            await _context.DisposeAsync();
            await _connection.DisposeAsync();
        }
    }
}

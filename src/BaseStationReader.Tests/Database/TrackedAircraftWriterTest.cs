using BaseStationReader.Data;
using BaseStationReader.Entities.Tracking;
using BaseStationReader.BusinessLogic.Database;
using System.Globalization;
using BaseStationReader.Interfaces.Database;
using BaseStationReader.Tests.Mocks;
using BaseStationReader.Entities.Api;

namespace BaseStationReader.Tests.Database
{
    [TestClass]
    public class AircraftWriterTest
    {
        private const string Address = "406A3D";
        private const string SecondAddress = "A9D9F7";
        private const string Callsign = "BAW486";
        private const decimal Altitude = 14325.0M;
        private const decimal GroundSpeed = 362.0M;
        private const decimal Track = 168.0M;
        private const decimal Latitude = 51.15067M;
        private const decimal Longitude = -0.52048M;
        private const decimal VerticalRate = 2624.0M;
        private const string Squawk = "7710";
        private readonly DateTime FirstSeen = DateTime.ParseExact("2023-08-22 17:51:59.551", "yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
        private readonly DateTime LastSeen = DateTime.ParseExact("2023-08-22 17:56:24.909", "yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);

        private IDatabaseManagementFactory _factory = null;
        private BaseStationReaderDbContext _context = null;

        [TestInitialize]
        public void TestInitialise()
        {
            var logger = new MockFileLogger();
            _context = BaseStationReaderDbContextFactory.CreateInMemoryDbContext();
            _factory = new DatabaseManagementFactory(logger, _context, 0);
        }

        [TestMethod]
        public async Task AddAndGetTestAsync()
        {
            await _factory.TrackedAircraftWriter.WriteAsync(new TrackedAircraft
            {
                Address = Address,
                FirstSeen = FirstSeen,
                LastSeen = LastSeen
            });

            var aircraft = await _factory.TrackedAircraftWriter.GetAsync(x => x.Address == Address);
            Assert.IsNotNull(aircraft);
            Assert.IsGreaterThan(0, aircraft.Id);
            Assert.AreEqual(Address, aircraft.Address);
            Assert.AreEqual(FirstSeen, aircraft.FirstSeen);
            Assert.AreEqual(LastSeen, aircraft.LastSeen);
        }


        [TestMethod]
        public async Task ListTestAsync()
        {
            await _factory.TrackedAircraftWriter.WriteAsync(new TrackedAircraft
            {
                Address = Address,
                FirstSeen = FirstSeen,
                LastSeen = LastSeen
            });

            var aircraft = await _factory.TrackedAircraftWriter.ListAsync(x => true);
            Assert.IsNotNull(aircraft);
            Assert.HasCount(1, aircraft);
            Assert.IsGreaterThan(0, aircraft.First().Id);
            Assert.AreEqual(Address, aircraft.First().Address);
            Assert.AreEqual(FirstSeen, aircraft.First().FirstSeen);
            Assert.AreEqual(LastSeen, aircraft.First().LastSeen);
        }

        [TestMethod]
        public async Task ListOrderingTestAsync()
        {
            var first = await _factory.TrackedAircraftWriter.WriteAsync(new TrackedAircraft
            {
                Address = Address,
                FirstSeen = FirstSeen,
                LastSeen = LastSeen,
                Status = TrackingStatus.Inactive
            });

            var second = await _factory.TrackedAircraftWriter.WriteAsync(new TrackedAircraft
            {
                Address = Address,
                FirstSeen = FirstSeen,
                LastSeen = LastSeen.AddMinutes(10),
                Status = TrackingStatus.Active
            });

            Assert.AreNotEqual(first.Id, second.Id);

            var aircraft = await _factory.TrackedAircraftWriter.ListAsync(x => true);
            Assert.IsNotNull(aircraft);
            Assert.HasCount(2, aircraft);
            Assert.AreEqual(second.Id, aircraft[0].Id);
            Assert.AreEqual(first.Id, aircraft[1].Id);
        }


        [TestMethod]
        public async Task UpdateTestAsync()
        {
            var initial = await _factory.TrackedAircraftWriter.WriteAsync(new TrackedAircraft
            {
                Address = Address,
                FirstSeen = FirstSeen,
                LastSeen = LastSeen
            });

            await _factory.TrackedAircraftWriter.WriteAsync(new TrackedAircraft
            {
                Id = initial.Id,
                Address = Address,
                Callsign = Callsign,
                Altitude = Altitude,
                GroundSpeed = GroundSpeed,
                Track = Track,
                Latitude = Latitude,
                Longitude = Longitude,
                VerticalRate = VerticalRate,
                Squawk = Squawk,
                FirstSeen = FirstSeen,
                LastSeen = LastSeen
            });

            var aircraft = await _factory.TrackedAircraftWriter.ListAsync(x => true);
            Assert.IsNotNull(aircraft);
            Assert.HasCount(1, aircraft);
            Assert.AreEqual(initial.Id, aircraft.First().Id);
            Assert.AreEqual(Address, aircraft.First().Address);
            Assert.AreEqual(FirstSeen, aircraft.First().FirstSeen);
            Assert.AreEqual(LastSeen, aircraft.First().LastSeen);
            Assert.AreEqual(Altitude, aircraft.First().Altitude);
            Assert.AreEqual(GroundSpeed, aircraft.First().GroundSpeed);
            Assert.AreEqual(Track, aircraft.First().Track);
            Assert.AreEqual(Latitude, aircraft.First().Latitude);
            Assert.AreEqual(Longitude, aircraft.First().Longitude);
            Assert.AreEqual(VerticalRate, aircraft.First().VerticalRate);
            Assert.AreEqual(Squawk, aircraft.First().Squawk);
        }

        [TestMethod]
        public async Task ExistingSightingIsNotReturnedAsCandidateTestAsync()
        {
            var tracked = await _factory.TrackedAircraftWriter.WriteAsync(new TrackedAircraft
            {
                Address = Address,
                Callsign = Callsign,
                FirstSeen = FirstSeen,
                LastSeen = LastSeen,
                Status = TrackingStatus.Active
            });

            var candidates = await _factory.TrackedAircraftWriter.ListSightingCreationCandidatesAsync();
            Assert.HasCount(1, candidates);

            var aircraft = new Aircraft
            {
                Address = Address,
                Registration = "G-TEST"
            };
            var flight = new Flight
            {
                Callsign = Callsign,
                IATA = "BA486"
            };
            await _context.Aircraft.AddAsync(aircraft);
            await _context.Flights.AddAsync(flight);
            await _context.SaveChangesAsync();
            await _context.Sightings.AddAsync(new Sighting
            {
                AircraftId = aircraft.Id,
                FlightId = flight.Id,
                Timestamp = tracked.FirstSeen
            });
            await _context.SaveChangesAsync();

            candidates = await _factory.TrackedAircraftWriter.ListSightingCreationCandidatesAsync();
            Assert.IsEmpty(candidates);
        }

        [TestMethod]
        public async Task AddSecondTestAsync()
        {
            await _factory.TrackedAircraftWriter.WriteAsync(new TrackedAircraft
            {
                Address = Address,
                FirstSeen = FirstSeen,
                LastSeen = LastSeen
            });

            await _factory.TrackedAircraftWriter.WriteAsync(new TrackedAircraft
            {
                Address = SecondAddress,
                FirstSeen = FirstSeen,
                LastSeen = LastSeen
            });

            var first = await _factory.TrackedAircraftWriter.GetAsync(x => x.Address == Address);
            Assert.IsNotNull(first);
            Assert.IsGreaterThan(0, first.Id);
            Assert.AreEqual(Address, first.Address);
            Assert.AreEqual(FirstSeen, first.FirstSeen);
            Assert.AreEqual(LastSeen, first.LastSeen);

            var second = await _factory.TrackedAircraftWriter.GetAsync(x => x.Address == SecondAddress);
            Assert.IsNotNull(second);
            Assert.IsGreaterThan(0, second.Id);
            Assert.AreNotEqual(first.Id, second.Id);
            Assert.AreEqual(SecondAddress, second.Address);
            Assert.AreEqual(FirstSeen, second.FirstSeen);
            Assert.AreEqual(LastSeen, second.LastSeen);
        }
    }
}

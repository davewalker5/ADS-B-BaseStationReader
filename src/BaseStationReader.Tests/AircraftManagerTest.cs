using BaseStationReader.Data;
using BaseStationReader.Entities.Tracking;
using BaseStationReader.BusinessLogic.Database;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace BaseStationReader.Tests
{
    [ExcludeFromCodeCoverage]
    [TestClass]
    public class AircraftManagerTest
    {
        private const string Address = "406A3D";
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

#pragma warning disable CS8618
        private AircraftWriter _manager;
        private Aircraft _aircraft;
#pragma warning restore CS8618


        [TestInitialize]
        public async Task TestInitialize()
        {
            BaseStationReaderDbContext context = BaseStationReaderDbContextFactory.CreateInMemoryDbContext();
            _manager = new AircraftWriter(context);

            var aircraft = new Aircraft
            {
                Address = Address,
                FirstSeen = FirstSeen,
                LastSeen = FirstSeen
            };
            _aircraft = await _manager.WriteAsync(aircraft);
        }

        [TestMethod]
        public async Task AddAndGetTest()
        {
            var aircraft = await _manager.GetAsync(x => x.Address == Address);

            Assert.IsNotNull(aircraft);
            Assert.AreEqual(_aircraft.Id, aircraft.Id);
            Assert.AreEqual(Address, aircraft.Address);
            Assert.AreEqual(FirstSeen, aircraft.FirstSeen);
            Assert.AreEqual(FirstSeen, aircraft.LastSeen);
        }

        [TestMethod]
        public async Task ListTest()
        {
            var aircraft = await _manager.ListAsync(x => x.Address == Address);

            Assert.IsNotNull(aircraft);
            Assert.AreEqual(1, aircraft.Count);
            Assert.AreEqual(_aircraft.Id, aircraft.First().Id);
            Assert.AreEqual(Address, aircraft.First().Address);
            Assert.AreEqual(FirstSeen, aircraft.First().FirstSeen);
            Assert.AreEqual(FirstSeen, aircraft.First().LastSeen);
        }

        [TestMethod]
        public async Task UpdateTest()
        {
            var aircraft = await _manager.GetAsync(x => x.Address == Address);
            aircraft.Callsign = Callsign;
            aircraft.Altitude = Altitude;
            aircraft.GroundSpeed = GroundSpeed;
            aircraft.Track = Track;
            aircraft.Latitude = Latitude;
            aircraft.Longitude = Longitude;
            aircraft.VerticalRate = VerticalRate;
            aircraft.Squawk = Squawk;
            aircraft.LastSeen = LastSeen;
            await _manager.WriteAsync(aircraft);

            var retrieved = await _manager.GetAsync(x => x.Address == Address);

            Assert.IsNotNull(retrieved);
            Assert.AreEqual(aircraft.Id, retrieved.Id);
            Assert.AreEqual(Address, retrieved.Address);
            Assert.AreEqual(Altitude, retrieved.Altitude);
            Assert.AreEqual(GroundSpeed, retrieved.GroundSpeed);
            Assert.AreEqual(Track, retrieved.Track);
            Assert.AreEqual(Latitude, retrieved.Latitude);
            Assert.AreEqual(Longitude, retrieved.Longitude);
            Assert.AreEqual(VerticalRate, retrieved.VerticalRate);
            Assert.AreEqual(Squawk, retrieved.Squawk);
            Assert.AreEqual(FirstSeen, retrieved.FirstSeen);
            Assert.AreEqual(LastSeen, retrieved.LastSeen);
        }
    }
}
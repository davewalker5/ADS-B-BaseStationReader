using BaseStationReader.Data;
using BaseStationReader.Interfaces.Tracking;
using BaseStationReader.Entities.Tracking;
using BaseStationReader.BusinessLogic.Database;
using System.Globalization;

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

        private ITrackedAircraftWriter _writer = null;

        [TestInitialize]
        public void TestInitialise()
        {
            BaseStationReaderDbContext context = BaseStationReaderDbContextFactory.CreateInMemoryDbContext();
            _writer = new TrackedAircraftWriter(context);
        }

        [TestMethod]
        public async Task AddAndGetTest()
        {
            await _writer.WriteAsync(new TrackedAircraft
            {
                Address = Address,
                FirstSeen = FirstSeen,
                LastSeen = LastSeen
            });

            var aircraft = await _writer.GetAsync(x => x.Address == Address);
            Assert.IsNotNull(aircraft);
            Assert.IsGreaterThan(0, aircraft.Id);
            Assert.AreEqual(Address, aircraft.Address);
            Assert.AreEqual(FirstSeen, aircraft.FirstSeen);
            Assert.AreEqual(LastSeen, aircraft.LastSeen);
        }


        [TestMethod]
        public async Task ListTest()
        {
            await _writer.WriteAsync(new TrackedAircraft
            {
                Address = Address,
                FirstSeen = FirstSeen,
                LastSeen = LastSeen
            });

            var aircraft = await _writer.ListAsync(x => true);
            Assert.IsNotNull(aircraft);
            Assert.HasCount(1, aircraft);
            Assert.IsTrue(aircraft.First().Id > 0);
            Assert.AreEqual(Address, aircraft.First().Address);
            Assert.AreEqual(FirstSeen, aircraft.First().FirstSeen);
            Assert.AreEqual(LastSeen, aircraft.First().LastSeen);
        }

        [TestMethod]
        public async Task ListOrderingTest()
        {
            var first = await _writer.WriteAsync(new TrackedAircraft
            {
                Address = Address,
                FirstSeen = FirstSeen,
                LastSeen = LastSeen
            });

            var second = await _writer.WriteAsync(new TrackedAircraft
            {
                Address = Address,
                FirstSeen = FirstSeen,
                LastSeen = LastSeen.AddMinutes(10)
            });

            Assert.AreNotEqual(first.Id, second.Id);

            var aircraft = await _writer.ListAsync(x => true);
            Assert.IsNotNull(aircraft);
            Assert.HasCount(2, aircraft);
            Assert.AreEqual(second.Id, aircraft[0].Id);
            Assert.AreEqual(first.Id, aircraft[1].Id);
        }


        [TestMethod]
        public async Task UpdateTest()
        {
            var initial = await _writer.WriteAsync(new TrackedAircraft
            {
                Address = Address,
                FirstSeen = FirstSeen,
                LastSeen = LastSeen
            });

            await _writer.WriteAsync(new TrackedAircraft
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

            var aircraft = await _writer.ListAsync(x => true);
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
        public async Task SetTrackedAircraftTimestampAsyncTest()
        {
            var initial = await _writer.WriteAsync(new TrackedAircraft
            {
                Address = Address,
                FirstSeen = FirstSeen,
                LastSeen = LastSeen
            });

            Assert.IsNull(initial.LookupTimestamp);

            _ = await _writer.SetLookupTimestamp(Address);

            var aircraft = await _writer.ListAsync(x => true);
            Assert.IsNotNull(aircraft);
            Assert.HasCount(1, aircraft);
            Assert.IsNotNull(initial.LookupTimestamp);
        }

        [TestMethod]
        public async Task AddSecondTest()
        {
            await _writer.WriteAsync(new TrackedAircraft
            {
                Address = Address,
                FirstSeen = FirstSeen,
                LastSeen = LastSeen
            });

            await _writer.WriteAsync(new TrackedAircraft
            {
                Address = SecondAddress,
                FirstSeen = FirstSeen,
                LastSeen = LastSeen
            });

            var first = await _writer.GetAsync(x => x.Address == Address);
            Assert.IsNotNull(first);
            Assert.IsGreaterThan(0, first.Id);
            Assert.AreEqual(Address, first.Address);
            Assert.AreEqual(FirstSeen, first.FirstSeen);
            Assert.AreEqual(LastSeen, first.LastSeen);

            var second = await _writer.GetAsync(x => x.Address == SecondAddress);
            Assert.IsNotNull(second);
            Assert.IsGreaterThan(0, second.Id);
            Assert.AreNotEqual(first.Id, second.Id);
            Assert.AreEqual(SecondAddress, second.Address);
            Assert.AreEqual(FirstSeen, second.FirstSeen);
            Assert.AreEqual(LastSeen, second.LastSeen);
        }
    }
}

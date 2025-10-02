using BaseStationReader.Data;
using BaseStationReader.Entities.Tracking;
using BaseStationReader.BusinessLogic.Database;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using BaseStationReader.Tests.Mocks;
using BaseStationReader.Interfaces.Database;

namespace BaseStationReader.Tests.Database
{
    [TestClass]
    public class PositionWriterTest
    {
        private const string Address = "406A3D";
        private const decimal Latitude = 51.15067M;
        private const decimal Longitude = -0.52048M;
        private const decimal SecondLongitude = -0.53M;
        private readonly DateTime FirstSeen = DateTime.ParseExact("2023-08-22 17:51:59.551", "yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
        private readonly DateTime LastSeen = DateTime.ParseExact("2023-08-22 17:56:24.909", "yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);

        private IPositionWriter _writer = null;
        private int _aircraftId = 0;

        [TestInitialize]
        public async Task TestInitialise()
        {
            BaseStationReaderDbContext context = BaseStationReaderDbContextFactory.CreateInMemoryDbContext();

            TrackedAircraftWriter aircraftWriter = new TrackedAircraftWriter(context);
            _ = await aircraftWriter.WriteAsync(new TrackedAircraft
            {
                Address = Address,
                FirstSeen = FirstSeen,
                LastSeen = LastSeen
            });

            var logger = new MockFileLogger();
            _writer = new PositionWriter(context);
        }

        [TestMethod]
        public async Task AddAndGetTest()
        {
            await _writer.WriteAsync(new AircraftPosition
            {
                AircraftId = _aircraftId,
                Latitude = Latitude,
                Longitude = Longitude,
                Timestamp = DateTime.Now
            });

            var position = await _writer.GetAsync(x => x.AircraftId == _aircraftId);
            Assert.IsNotNull(position);
            Assert.IsGreaterThan(0, position.Id);
            Assert.AreEqual(_aircraftId, position.AircraftId);
            Assert.AreEqual(Latitude, position.Latitude);
            Assert.AreEqual(Longitude, position.Longitude);
        }


        [TestMethod]
        public async Task ListTest()
        {
            await _writer.WriteAsync(new AircraftPosition
            {
                AircraftId = _aircraftId,
                Latitude = Latitude,
                Longitude = Longitude,
                Timestamp = DateTime.Now
            });

            var positions = await _writer.ListAsync(x => true);
            Assert.IsNotNull(positions);
            Assert.HasCount(1, positions);
            Assert.IsGreaterThan(0, positions.First().Id);
            Assert.AreEqual(_aircraftId, positions.First().AircraftId);
            Assert.AreEqual(Latitude, positions.First().Latitude);
            Assert.AreEqual(Longitude, positions.First().Longitude);
        }

        [TestMethod]
        public async Task UpdateTest()
        {
            var initial = await _writer.WriteAsync(new AircraftPosition
            {
                AircraftId = _aircraftId,
                Latitude = Latitude,
                Longitude = Longitude,
                Timestamp = DateTime.Now
            });

            await _writer.WriteAsync(new AircraftPosition
            {
                Id = initial.Id,
                Latitude = Latitude,
                Longitude = SecondLongitude,
                Timestamp = DateTime.Now
            });

            var positions = await _writer.ListAsync(x => true);
            Assert.IsNotNull(positions);
            Assert.HasCount(1, positions);
            Assert.IsGreaterThan(0, positions.First().Id);
            Assert.AreEqual(_aircraftId, positions.First().AircraftId);
            Assert.AreEqual(Latitude, positions.First().Latitude);
            Assert.AreEqual(SecondLongitude, positions.First().Longitude);
        }

        [TestMethod]
        public async Task AddSecondTest()
        {
            var writtenFirst = await _writer.WriteAsync(new AircraftPosition
            {
                AircraftId = _aircraftId,
                Latitude = Latitude,
                Longitude = Longitude,
                Timestamp = DateTime.Now
            });

            var writtenSecond = await _writer.WriteAsync(new AircraftPosition
            {
                Latitude = Latitude,
                Longitude = SecondLongitude,
                Timestamp = DateTime.Now
            });

            var first = await _writer.GetAsync(x => x.Id == writtenFirst.Id);
            Assert.IsNotNull(first);
            Assert.AreEqual(writtenFirst.Id, first.Id);
            Assert.AreEqual(_aircraftId, first.AircraftId);
            Assert.AreEqual(Latitude, first.Latitude);
            Assert.AreEqual(Longitude, first.Longitude);

            var second = await _writer.GetAsync(x => x.Id == writtenSecond.Id);
            Assert.IsNotNull(second);
            Assert.AreEqual(writtenSecond.Id, second.Id);
            Assert.AreEqual(_aircraftId, second.AircraftId);
            Assert.AreEqual(Latitude, second.Latitude);
            Assert.AreEqual(SecondLongitude, second.Longitude);
        }
    }
}

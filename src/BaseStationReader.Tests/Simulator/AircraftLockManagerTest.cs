using BaseStationReader.Data;
using BaseStationReader.Entities.Tracking;
using BaseStationReader.BusinessLogic.Database;
using BaseStationReader.Interfaces.Database;

namespace BaseStationReader.Tests.Simulator
{
    [TestClass]
    public class AircraftLockManagerTest
    {
        private BaseStationReaderDbContext _context = null;
        private IDatabaseManagementFactory _factory = null;
        private const int TimeToLockMs = 600000;

        private const string Address = "406A3D";

        [TestInitialize]
        public void TestInitialise()
        {
            _context = BaseStationReaderDbContextFactory.CreateInMemoryDbContext();
            _factory = new DatabaseManagementFactory(_context, TimeToLockMs);
        }

        [TestMethod]
        public async Task GetActiveAircraftTest()
        {
            var added = await _factory.TrackedAircraftWriter.WriteAsync(new TrackedAircraft
            {
                Address = Address,
                FirstSeen = DateTime.Now.AddMinutes(-10),
                LastSeen = DateTime.Now
            });

            var active = await _factory.AircraftLockManager.GetActiveAircraftAsync(Address);
            Assert.IsNotNull(active);
            Assert.AreEqual(added.Id, active.Id);
        }

        [TestMethod]
        public async Task GetInactiveAircraftTest()
        {
            await _factory.TrackedAircraftWriter.WriteAsync(new TrackedAircraft
            {
                Address = Address,
                FirstSeen = DateTime.Now.AddMinutes(-20),
                LastSeen = DateTime.Now.AddMinutes(-15)
            });

            var active = await _factory.AircraftLockManager.GetActiveAircraftAsync(Address);
            Assert.IsNull(active);
        }

        [TestMethod]
        public async Task InactiveAircraftIsLockedTest()
        {
            var aircraft = await _factory.TrackedAircraftWriter.WriteAsync(new TrackedAircraft
            {
                Address = Address,
                FirstSeen = DateTime.Now.AddMinutes(-20),
                LastSeen = DateTime.Now.AddMinutes(-15)
            });

            Assert.IsGreaterThan(0, aircraft.Id);
            Assert.AreNotEqual(TrackingStatus.Locked, aircraft.Status);

            var active = await _factory.AircraftLockManager.GetActiveAircraftAsync(Address);
            Assert.IsNull(active);

            var retrieved = await _factory.TrackedAircraftWriter.GetAsync(x => x.Address == Address);
            Assert.IsNotNull(retrieved);
            Assert.AreEqual(aircraft.Id, retrieved.Id);
            Assert.AreEqual(TrackingStatus.Locked, aircraft.Status);
        }

        [TestMethod]
        public async Task GetMissingAircraftTest()
        {
            var active = await _factory.AircraftLockManager.GetActiveAircraftAsync("000000");
            Assert.IsNull(active);
        }
    }
}

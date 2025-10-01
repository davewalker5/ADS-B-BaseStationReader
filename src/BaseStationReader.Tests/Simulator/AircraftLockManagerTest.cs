using BaseStationReader.Data;
using BaseStationReader.Interfaces.Tracking;
using BaseStationReader.Entities.Tracking;
using BaseStationReader.BusinessLogic.Database;
using System.Diagnostics.CodeAnalysis;
using BaseStationReader.Interfaces.Database;

namespace BaseStationReader.Tests.Simulator
{
    [ExcludeFromCodeCoverage]
    [TestClass]
    public class AircraftLockManagerTest
    {
        private BaseStationReaderDbContext _context = null;
        private ITrackedAircraftWriter _aircraftWriter = null;
        private IAircraftLockManager _aircraftLocker = null;
        private const int TimeToLockMs = 600000;

        private const string Address = "406A3D";

        [TestInitialize]
        public void TestInitialise()
        {
            _context = BaseStationReaderDbContextFactory.CreateInMemoryDbContext();
            _aircraftWriter = new TrackedAircraftWriter(_context);
            _aircraftLocker = new AircraftLockManager(_aircraftWriter, TimeToLockMs);
        }

        [TestMethod]
        public async Task GetActiveAircraftTest()
        {
            var added = await _aircraftWriter.WriteAsync(new TrackedAircraft
            {
                Address = Address,
                FirstSeen = DateTime.Now.AddMinutes(-10),
                LastSeen = DateTime.Now
            });

            var active = await _aircraftLocker.GetActiveAircraftAsync(Address);
            Assert.IsNotNull(active);
            Assert.AreEqual(added.Id, active.Id);
        }

        [TestMethod]
        public async Task GetInactiveAircraftTest()
        {
            await _aircraftWriter.WriteAsync(new TrackedAircraft
            {
                Address = Address,
                FirstSeen = DateTime.Now.AddMinutes(-20),
                LastSeen = DateTime.Now.AddMinutes(-15)
            });

            var active = await _aircraftLocker.GetActiveAircraftAsync(Address);
            Assert.IsNull(active);
        }

        [TestMethod]
        public async Task InactiveAircraftIsLockedTest()
        {
            var aircraft = await _aircraftWriter.WriteAsync(new TrackedAircraft
            {
                Address = Address,
                FirstSeen = DateTime.Now.AddMinutes(-20),
                LastSeen = DateTime.Now.AddMinutes(-15)
            });

            Assert.IsTrue(aircraft.Id > 0);
            Assert.AreNotEqual(TrackingStatus.Locked, aircraft.Status);

            var active = await _aircraftLocker.GetActiveAircraftAsync(Address);
            Assert.IsNull(active);

            var retrieved = await _aircraftWriter.GetAsync(x => x.Address == Address);
            Assert.IsNotNull(retrieved);
            Assert.AreEqual(aircraft.Id, retrieved.Id);
            Assert.AreEqual(TrackingStatus.Locked, aircraft.Status);
        }

        [TestMethod]
        public async Task GetMissingAircraftTest()
        {
            var active = await _aircraftLocker.GetActiveAircraftAsync("000000");
            Assert.IsNull(active);
        }
    }
}

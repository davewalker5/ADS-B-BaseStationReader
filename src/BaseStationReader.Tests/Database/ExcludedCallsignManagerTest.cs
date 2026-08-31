using BaseStationReader.Data;
using BaseStationReader.BusinessLogic.Database;
using BaseStationReader.Interfaces.Database;
using BaseStationReader.Entities.Tracking;
using Microsoft.EntityFrameworkCore;

namespace BaseStationReader.Tests.Database
{
    [TestClass]
    public class ExcludedCallsignManagerTest
    {
        private const string Callsign = "ABC123";

        private IExcludedCallsignManager _manager = null;

        [TestInitialize]
        public void InitialiseAsync()
        {
            BaseStationReaderDbContext context = BaseStationReaderDbContextFactory.CreateInMemoryDbContext();
            _manager = new ExcludedCallsignManager(context);
        }

        [TestMethod]
        public async Task IsExcludedTestAsync()
        {
            await _manager.AddAsync(Callsign);
            var excluded = await _manager.IsExcludedAsync(Callsign);
            Assert.IsTrue(excluded);
        }

        [TestMethod]
        public async Task AddNormalisesCallsignAndDoesNotDuplicateTestAsync()
        {
            await _manager.AddAsync(" baw123 ");
            await _manager.AddAsync("BAW123");

            var exclusions = await _manager.SearchAsync(null);

            Assert.HasCount(1, exclusions);
            Assert.AreEqual("BAW123", exclusions[0].Callsign);
        }

        [TestMethod]
        public async Task IsNotExcludedTestAsync()
        {
            var excluded = await _manager.IsExcludedAsync(Callsign);
            Assert.IsFalse(excluded);
        }

        [TestMethod]
        public async Task ListTestAsync()
        {
            await _manager.AddAsync(Callsign);
            var exclusions = await _manager.ListAsync(x => true);
            Assert.IsNotNull(exclusions);
            Assert.HasCount(1, exclusions);
            Assert.AreEqual(Callsign, exclusions[0].Callsign);
        }

        [TestMethod]
        public async Task ListEmptyTestAsync()
        {
            var exclusions = await _manager.ListAsync(x => true);
            Assert.IsNotNull(exclusions);
            Assert.IsEmpty(exclusions);
        }

        [TestMethod]
        public async Task SearchNormalisesPartialCallsignAndOrdersResultsTestAsync()
        {
            await _manager.AddAsync("BAW999");
            await _manager.AddAsync("BAW123");
            await _manager.AddAsync("EZY123");

            var exclusions = await _manager.SearchAsync(" baw ");

            Assert.HasCount(2, exclusions);
            Assert.AreEqual("BAW123", exclusions[0].Callsign);
            Assert.AreEqual("BAW999", exclusions[1].Callsign);
        }

        [TestMethod]
        public async Task SearchWithoutCallsignReturnsAllTestAsync()
        {
            await _manager.AddAsync(Callsign);

            var exclusions = await _manager.SearchAsync(null);

            Assert.HasCount(1, exclusions);
            Assert.AreEqual(Callsign, exclusions[0].Callsign);
        }

        [TestMethod]
        public async Task DeleteTestAsync()
        {
            await _manager.AddAsync(Callsign);
            var excluded = await _manager.IsExcludedAsync(Callsign);
            Assert.IsTrue(excluded);

            await _manager.DeleteAsync(Callsign);
            excluded = await _manager.IsExcludedAsync(Callsign);
            Assert.IsFalse(excluded);
        }

        [TestMethod]
        public async Task PurgeTrackingDataDeletesExcludedCallsignAircraftAndPositionsOnlyTestAsync()
        {
            var context = BaseStationReaderDbContextFactory.CreateInMemoryDbContext();
            var manager = new ExcludedCallsignManager(context);
            await manager.AddAsync(Callsign);

            var excludedAircraft = CreateTrackedAircraft("ABC001", Callsign);
            var retainedAircraft = CreateTrackedAircraft("DEF456", "BAW456");
            context.TrackedAircraft.AddRange(excludedAircraft, retainedAircraft);
            await context.SaveChangesAsync();
            context.Positions.AddRange(
                CreatePosition(excludedAircraft),
                CreatePosition(retainedAircraft));
            await context.SaveChangesAsync();

            var deleted = await manager.PurgeTrackingDataAsync();

            Assert.AreEqual(1, deleted);
            Assert.HasCount(1, await context.TrackedAircraft.ToListAsync());
            Assert.AreEqual("BAW456", (await context.TrackedAircraft.SingleAsync()).Callsign);
            Assert.HasCount(1, await context.Positions.ToListAsync());
            Assert.AreEqual(retainedAircraft.Id, (await context.Positions.SingleAsync()).AircraftId);
            Assert.HasCount(1, await context.ExcludedCallsigns.ToListAsync());
        }

        private static TrackedAircraft CreateTrackedAircraft(string address, string callsign)
            => new()
            {
                Address = address,
                Callsign = callsign,
                FirstSeen = DateTime.UtcNow,
                LastSeen = DateTime.UtcNow,
                Status = TrackingStatus.Active
            };

        private static AircraftPosition CreatePosition(TrackedAircraft aircraft)
            => new()
            {
                Address = aircraft.Address,
                AircraftId = aircraft.Id,
                Aircraft = aircraft,
                Timestamp = DateTime.UtcNow
            };
    }
}

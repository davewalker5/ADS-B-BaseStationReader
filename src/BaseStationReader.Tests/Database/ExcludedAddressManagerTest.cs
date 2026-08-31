using BaseStationReader.Data;
using BaseStationReader.BusinessLogic.Database;
using BaseStationReader.Interfaces.Database;
using BaseStationReader.Entities.Tracking;
using Microsoft.EntityFrameworkCore;

namespace BaseStationReader.Tests.Database
{
    [TestClass]
    public class ExcludedAddressManagerTest
    {
        private const string Address = "ABC123";

        private IExcludedAddressManager _manager = null;

        [TestInitialize]
        public void InitialiseAsync()
        {
            BaseStationReaderDbContext context = BaseStationReaderDbContextFactory.CreateInMemoryDbContext();
            _manager = new ExcludedAddressManager(context);
        }

        [TestMethod]
        public async Task IsExcludedTestAsync()
        {
            await _manager.AddAsync(Address);
            var excluded = await _manager.IsExcludedAsync(Address);
            Assert.IsTrue(excluded);
        }

        [TestMethod]
        public async Task AddNormalisesAddressAndDoesNotDuplicateTestAsync()
        {
            await _manager.AddAsync(" abc123 ");
            await _manager.AddAsync("ABC123");

            var exclusions = await _manager.SearchAsync(null);

            Assert.HasCount(1, exclusions);
            Assert.AreEqual("ABC123", exclusions[0].Address);
        }

        [TestMethod]
        public async Task IsNotExcludedTestAsync()
        {
            var excluded = await _manager.IsExcludedAsync(Address);
            Assert.IsFalse(excluded);
        }

        [TestMethod]
        public async Task ListTestAsync()
        {
            await _manager.AddAsync(Address);
            var exclusions = await _manager.ListAsync(x => true);
            Assert.IsNotNull(exclusions);
            Assert.HasCount(1, exclusions);
            Assert.AreEqual(Address, exclusions[0].Address);
        }

        [TestMethod]
        public async Task ListEmptyTestAsync()
        {
            var exclusions = await _manager.ListAsync(x => true);
            Assert.IsNotNull(exclusions);
            Assert.IsEmpty(exclusions);
        }

        [TestMethod]
        public async Task SearchNormalisesPartialAddressAndOrdersResultsTestAsync()
        {
            await _manager.AddAsync("ABC999");
            await _manager.AddAsync("ABC123");
            await _manager.AddAsync("DEF123");

            var exclusions = await _manager.SearchAsync(" abc ");

            Assert.HasCount(2, exclusions);
            Assert.AreEqual("ABC123", exclusions[0].Address);
            Assert.AreEqual("ABC999", exclusions[1].Address);
        }

        [TestMethod]
        public async Task SearchWithoutAddressReturnsAllTestAsync()
        {
            await _manager.AddAsync(Address);

            var exclusions = await _manager.SearchAsync(null);

            Assert.HasCount(1, exclusions);
            Assert.AreEqual(Address, exclusions[0].Address);
        }

        [TestMethod]
        public async Task DeleteTestAsync()
        {
            await _manager.AddAsync(Address);
            var excluded = await _manager.IsExcludedAsync(Address);
            Assert.IsTrue(excluded);

            await _manager.DeleteAsync(Address);
            excluded = await _manager.IsExcludedAsync(Address);
            Assert.IsFalse(excluded);
        }

        [TestMethod]
        public async Task PurgeTrackingDataDeletesExcludedAircraftAndPositionsOnlyTestAsync()
        {
            var context = BaseStationReaderDbContextFactory.CreateInMemoryDbContext();
            var manager = new ExcludedAddressManager(context);
            await manager.AddAsync(Address);

            var excludedAircraft = CreateTrackedAircraft(Address);
            var retainedAircraft = CreateTrackedAircraft("DEF456");
            context.TrackedAircraft.AddRange(excludedAircraft, retainedAircraft);
            await context.SaveChangesAsync();
            context.Positions.AddRange(
                CreatePosition(excludedAircraft),
                CreatePosition(retainedAircraft));
            await context.SaveChangesAsync();

            var deleted = await manager.PurgeTrackingDataAsync();

            Assert.AreEqual(1, deleted);
            Assert.HasCount(1, await context.TrackedAircraft.ToListAsync());
            Assert.AreEqual("DEF456", (await context.TrackedAircraft.SingleAsync()).Address);
            Assert.HasCount(1, await context.Positions.ToListAsync());
            Assert.AreEqual(retainedAircraft.Id, (await context.Positions.SingleAsync()).AircraftId);
            Assert.HasCount(1, await context.ExcludedAddresses.ToListAsync());
        }

        private static TrackedAircraft CreateTrackedAircraft(string address)
            => new()
            {
                Address = address,
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

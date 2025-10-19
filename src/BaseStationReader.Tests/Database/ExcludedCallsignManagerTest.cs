using BaseStationReader.Data;
using BaseStationReader.BusinessLogic.Database;
using BaseStationReader.Interfaces.Database;

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
        public async Task DeleteTestAsync()
        {
            await _manager.AddAsync(Callsign);
            var excluded = await _manager.IsExcludedAsync(Callsign);
            Assert.IsTrue(excluded);

            await _manager.DeleteAsync(Callsign);
            excluded = await _manager.IsExcludedAsync(Callsign);
            Assert.IsFalse(excluded);
        }
    }
}

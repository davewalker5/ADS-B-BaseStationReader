using BaseStationReader.Data;
using BaseStationReader.BusinessLogic.Database;
using BaseStationReader.Interfaces.Database;

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
        public async Task DeleteTestAsync()
        {
            await _manager.AddAsync(Address);
            var excluded = await _manager.IsExcludedAsync(Address);
            Assert.IsTrue(excluded);

            await _manager.DeleteAsync(Address);
            excluded = await _manager.IsExcludedAsync(Address);
            Assert.IsFalse(excluded);
        }
    }
}

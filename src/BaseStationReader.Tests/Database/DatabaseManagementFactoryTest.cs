using BaseStationReader.BusinessLogic.Database;
using BaseStationReader.Data;
using BaseStationReader.Interfaces.Database;

namespace BaseStationReader.Tests.Database
{
    [TestClass]
    public class DatabaseManagementFactoryTest
    {
        private const string Address = "ABC123";
        private const string Callsign = "DEF456";

        private BaseStationReaderDbContext _context;
        private IDatabaseManagementFactory _factory;

        [TestInitialize]
        public void Initialise()
        {
            _context = BaseStationReaderDbContextFactory.CreateInMemoryDbContext();
            _factory = new DatabaseManagementFactory(null, _context, 0, 0);
        }
    
        [TestMethod]
        public void GetContextTest()
        {
            var retrieved = _factory.Context<BaseStationReaderDbContext>();
            Assert.IsNotNull(retrieved);
            Assert.AreEqual(_context, retrieved);
        }

        [TestMethod]
        public async Task ExcludedAddressManagerTestAsync()
        {

            await _factory.ExcludedAddressManager.AddAsync(Address);
            var exclusions = await _factory.ExcludedAddressManager.ListAsync(x => true);
            Assert.IsNotNull(exclusions);
            Assert.HasCount(1, exclusions);
            Assert.AreEqual(Address, exclusions[0].Address);
        }

        [TestMethod]
        public async Task ExcludedCallsignManagerTestAsync()
        {

            await _factory.ExcludedCallsignManager.AddAsync(Callsign);
            var exclusions = await _factory.ExcludedCallsignManager.ListAsync(x => true);
            Assert.IsNotNull(exclusions);
            Assert.HasCount(1, exclusions);
            Assert.AreEqual(Callsign, exclusions[0].Callsign);
        }
    }
}
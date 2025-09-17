using BaseStationReader.Data;
using BaseStationReader.Entities.Interfaces;
using BaseStationReader.BusinessLogic.Database;

namespace BaseStationReader.Tests
{
    [TestClass]
    public class ManufacturerManagerTest
    {
        private const string Name = "Airbus";

        private IManufacturerManager _manager = null;

        [TestInitialize]
        public void TestInitialize()
        {
            BaseStationReaderDbContext context = BaseStationReaderDbContextFactory.CreateInMemoryDbContext();
            _manager = new ManufacturerManager(context);
            Task.Run(() => _manager.AddAsync(Name)).Wait();
        }

        [TestMethod]
        public async Task AddDuplicateTest()
        {
            await _manager!.AddAsync(Name);
            var manufacturers = await _manager.ListAsync(x => true);
            Assert.AreEqual(1, manufacturers.Count);
        }

        [TestMethod]
        public async Task AddAndGetTest()
        {
            var manufacturer = await _manager!.GetAsync(a => a.Name == Name);
            Assert.IsNotNull(manufacturer);
            Assert.IsTrue(manufacturer.Id > 0);
            Assert.AreEqual(Name, manufacturer.Name);
        }

        [TestMethod]
        public async Task GetMissingTest()
        {
            var manufacturer = await _manager!.GetAsync(a => a.Name == "Missing");
            Assert.IsNull(manufacturer);
        }

        [TestMethod]
        public async Task ListAllTest()
        {
            var manufacturers = await _manager!.ListAsync(x => true);
            Assert.AreEqual(1, manufacturers!.Count);
            Assert.AreEqual(Name, manufacturers.First().Name);
        }

        [TestMethod]
        public async Task ListMissingTest()
        {
            var manufacturers = await _manager!.ListAsync(e => e.Name == "Missing");
            Assert.AreEqual(0, manufacturers!.Count);
        }
    }
}

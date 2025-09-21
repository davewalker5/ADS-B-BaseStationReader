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
        public async Task TestInitialize()
        {
            BaseStationReaderDbContext context = BaseStationReaderDbContextFactory.CreateInMemoryDbContext();
            _manager = new ManufacturerManager(context);
            _ = await _manager.AddAsync(Name);
        }

        [TestMethod]
        public async Task AddDuplicateTest()
        {
            await _manager.AddAsync(Name);
            var manufacturers = await _manager.ListAsync(x => true);
            Assert.HasCount(1, manufacturers);
        }

        [TestMethod]
        public async Task AddAndGetTest()
        {
            var manufacturer = await _manager.GetAsync(a => a.Name == Name);
            Assert.IsNotNull(manufacturer);
            Assert.IsGreaterThan(0, manufacturer.Id);
            Assert.AreEqual(Name, manufacturer.Name);
        }

        [TestMethod]
        public async Task GetMissingTest()
        {
            var manufacturer = await _manager.GetAsync(a => a.Name == "Missing");
            Assert.IsNull(manufacturer);
        }

        [TestMethod]
        public async Task ListAllTest()
        {
            var manufacturers = await _manager.ListAsync(x => true);
            Assert.HasCount(1, manufacturers);
            Assert.AreEqual(Name, manufacturers.First().Name);
        }

        [TestMethod]
        public async Task ListMissingTest()
        {
            var manufacturers = await _manager.ListAsync(e => e.Name == "Missing");
            Assert.IsEmpty(manufacturers);
        }
    }
}

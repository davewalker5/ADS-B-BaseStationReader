using BaseStationReader.BusinessLogic.Database;
using BaseStationReader.BusinessLogic.Logging;
using BaseStationReader.Data;
using BaseStationReader.Interfaces.DataExchange;
using BaseStationReader.Interfaces.Database;
using BaseStationReader.Tests.Mocks;

namespace BaseStationReader.Tests.DataExchange
{
    [TestClass]
    public class ManufacturerImporterTest
    {

        private IDatabaseManagementFactory _factory;
        private IManufacturerImporter _importer;

        [TestInitialize]
        public void Initialise()
        {
            var context = BaseStationReaderDbContextFactory.CreateInMemoryDbContext();
            var logger = new MockFileLogger();
            _factory = new DatabaseManagementFactory(logger, context, 0, 0);
            _importer = new ManufacturerImporter(_factory);
        }

        [TestMethod]
        public async Task ImportTestAsync()
        {
            await _importer.ImportAsync("manufacturers.csv");
            var manufacturers = await _factory.ManufacturerManager.ListAsync(x => true);

            Assert.IsNotNull(manufacturers);
            Assert.HasCount(1, manufacturers);
            Assert.IsGreaterThan(0, manufacturers[0].Id);
            Assert.AreEqual("Airbus", manufacturers[0].Name);
        }

        [TestMethod]
        public async Task ImportEmptyFileTestAsync()
        {
            await _importer.ImportAsync("empty_manufacturers.csv");
            var manufacturers = await _factory.ManufacturerManager.ListAsync(x => true);

            Assert.IsNotNull(manufacturers);
            Assert.HasCount(0, manufacturers);
        }

        [TestMethod]
        public async Task ImportMissingFileTestAsync()
        {
            await _importer.ImportAsync("missing.csv");
            var manufacturers = await _factory.ManufacturerManager.ListAsync(x => true);

            Assert.IsNotNull(manufacturers);
            Assert.HasCount(0, manufacturers);
        }
    }
}
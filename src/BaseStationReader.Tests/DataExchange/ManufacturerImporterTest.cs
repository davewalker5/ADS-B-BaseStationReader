using BaseStationReader.BusinessLogic.Database;
using BaseStationReader.BusinessLogic.Logging;
using BaseStationReader.Data;
using BaseStationReader.Entities.Interfaces;
using BaseStationReader.Tests.Mocks;

namespace BaseStationReader.Tests.DataExchange
{
    [TestClass]
    public class ManufacturerImporterTest
    {
        private IManufacturerManager _manufacturerManager;
        private IManufacturerImporter _importer;

        [TestInitialize]
        public void Initialise()
        {
            var context = BaseStationReaderDbContextFactory.CreateInMemoryDbContext();
            var logger = new MockFileLogger();
            _manufacturerManager = new ManufacturerManager(context);
            _importer = new ManufacturerImporter(_manufacturerManager, logger);
        }

        [TestMethod]
        public async Task ImportTest()
        {
            await _importer.Import("manufacturers.csv");
            var manufacturers = await _manufacturerManager.ListAsync(x => true);

            Assert.IsNotNull(manufacturers);
            Assert.HasCount(1, manufacturers);
            Assert.IsGreaterThan(0, manufacturers[0].Id);
            Assert.AreEqual("Airbus", manufacturers[0].Name);
        }

        [TestMethod]
        public async Task ImportEmptyFileTest()
        {
            await _importer.Import("empty_manufacturers.csv");
            var manufacturers = await _manufacturerManager.ListAsync(x => true);

            Assert.IsNotNull(manufacturers);
            Assert.HasCount(0, manufacturers);
        }

        [TestMethod]
        public async Task ImportMissingFileTest()
        {
            await _importer.Import("missing.csv");
            var manufacturers = await _manufacturerManager.ListAsync(x => true);

            Assert.IsNotNull(manufacturers);
            Assert.HasCount(0, manufacturers);
        }
    }
}
using BaseStationReader.BusinessLogic.Database;
using BaseStationReader.BusinessLogic.Logging;
using BaseStationReader.Data;
using BaseStationReader.Interfaces.DataExchange;
using BaseStationReader.Interfaces.Database;
using BaseStationReader.Tests.Mocks;

namespace BaseStationReader.Tests.DataExchange
{
    [TestClass]
    public class ModelImporterTest
    {
        private IManufacturerManager _manufacturerManager;
        private IModelManager _modelManager;
        private IModelImporter _importer;

        [TestInitialize]
        public void Initialise()
        {
            var context = BaseStationReaderDbContextFactory.CreateInMemoryDbContext();
            var logger = new MockFileLogger();
            _manufacturerManager = new ManufacturerManager(context);
            _modelManager = new ModelManager(context);
            _importer = new ModelImporter(_manufacturerManager, _modelManager, logger);
        }

        [TestMethod]
        public async Task ImportTestAsync()
        {
            _ = await _manufacturerManager.AddAsync("Airbus");
            await _importer.ImportAsync("models.csv");
            var models = await _modelManager.ListAsync(x => true);

            Assert.IsNotNull(models);
            Assert.HasCount(1, models);
            Assert.IsGreaterThan(0, models[0].Id);
            Assert.AreEqual("A20N", models[0].ICAO);
            Assert.AreEqual("32N", models[0].IATA);
            Assert.AreEqual("Airbus A320neo", models[0].Name);
            Assert.AreEqual("Airbus", models[0].Manufacturer.Name);
        }

        [TestMethod]
        public async Task ImportWithoutManufacturerPresentTestAsync()
        {
            await _importer.ImportAsync("models.csv");
            var models = await _modelManager.ListAsync(x => true);

            Assert.IsNotNull(models);
            Assert.HasCount(0, models);
        }

        [TestMethod]
        public async Task ImportEmptyFileTestAsync()
        {
            await _importer.ImportAsync("empty_models.csv");
            var models = await _modelManager.ListAsync(x => true);

            Assert.IsNotNull(models);
            Assert.HasCount(0, models);
        }

        [TestMethod]
        public async Task ImportMissingFileTestAsync()
        {
            await _importer.ImportAsync("missing.csv");
            var models = await _modelManager.ListAsync(x => true);

            Assert.IsNotNull(models);
            Assert.HasCount(0, models);
        }
    }
}
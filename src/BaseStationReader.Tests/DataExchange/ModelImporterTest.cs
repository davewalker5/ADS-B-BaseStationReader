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


        private IDatabaseManagementFactory _factory;

        private IModelImporter _importer;

        [TestInitialize]
        public async Task InitialiseAsync()
        {
            var context = BaseStationReaderDbContextFactory.CreateInMemoryDbContext();
            var logger = new MockFileLogger();
            _factory = new DatabaseManagementFactory(logger, context, 0);
            _importer = new ModelImporter(_factory);
            await _factory.ProvenanceManager.AddAsync("TEST", "Test source", "N/A", "Models", "1", "N/A");
        }

        [TestMethod]
        public async Task ImportTestAsync()
        {
            _ = await _factory.ManufacturerManager.AddAsync("Airbus");
            await _importer.ImportAsync("models.csv");
            var models = await _factory.ModelManager.ListAsync(x => true);

            Assert.IsNotNull(models);
            Assert.HasCount(1, models);
            Assert.IsGreaterThan(0, models[0].Id);
            Assert.AreEqual("A20N", models[0].ICAO);
            Assert.AreEqual("32N", models[0].IATA);
            Assert.AreEqual("Airbus A320neo", models[0].Name);
            Assert.AreEqual("Airbus", models[0].Manufacturer.Name);
            Assert.AreEqual("TEST", models[0].Provenance.SourceRef);
        }

        [TestMethod]
        public async Task ImportAllowsEmptyCodeValueTestAsync()
        {
            _ = await _factory.ManufacturerManager.AddAsync("Airbus");
            await _importer.ImportAsync("models-null-code.csv");
            var models = await _factory.ModelManager.ListAsync(x => true);

            Assert.HasCount(1, models);
            Assert.IsNull(models[0].ICAO);
            Assert.AreEqual("32N", models[0].IATA);
            Assert.AreEqual("TEST", models[0].Provenance.SourceRef);
        }

        [TestMethod]
        public async Task ImportFailsWhenProvenanceIsMissingTestAsync()
        {
            _ = await _factory.ManufacturerManager.AddAsync("Airbus");
            var provenance = await _factory.ProvenanceManager.GetAsync(x => x.SourceRef == "TEST");
            await _factory.ProvenanceManager.DeleteAsync(provenance.Id);

            await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => _importer.ImportAsync("models.csv"));
            Assert.IsEmpty(await _factory.ModelManager.ListAsync(x => true));
        }

        [TestMethod]
        public async Task ImportWithoutManufacturerPresentTestAsync()
        {
            await _importer.ImportAsync("models.csv");
            var models = await _factory.ModelManager.ListAsync(x => true);

            Assert.IsNotNull(models);
            Assert.HasCount(0, models);
        }

        [TestMethod]
        public async Task ImportEmptyFileTestAsync()
        {
            await _importer.ImportAsync("empty_models.csv");
            var models = await _factory.ModelManager.ListAsync(x => true);

            Assert.IsNotNull(models);
            Assert.HasCount(0, models);
        }

        [TestMethod]
        public async Task ImportMissingFileTestAsync()
        {
            await _importer.ImportAsync("missing.csv");
            var models = await _factory.ModelManager.ListAsync(x => true);

            Assert.IsNotNull(models);
            Assert.HasCount(0, models);
        }
    }
}

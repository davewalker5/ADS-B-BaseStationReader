using BaseStationReader.BusinessLogic.Database;
using BaseStationReader.BusinessLogic.Import;
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
        public async Task InitialiseAsync()
        {
            var context = BaseStationReaderDbContextFactory.CreateInMemoryDbContext();
            var logger = new MockFileLogger();
            _factory = new DatabaseManagementFactory(logger, context, 0);
            _importer = new ManufacturerImporter(_factory);
            await _factory.ProvenanceManager.AddAsync("TEST", "Test source", "N/A", "Manufacturers", "1", "N/A");
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
            Assert.AreEqual("TEST", manufacturers[0].Provenance.SourceRef);
        }

        [TestMethod]
        public async Task ImportFailsWhenProvenanceIsMissingTestAsync()
        {
            var provenance = await _factory.ProvenanceManager.GetAsync(x => x.SourceRef == "TEST");
            await _factory.ProvenanceManager.DeleteAsync(provenance.Id);

            await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => _importer.ImportAsync("manufacturers.csv"));
            Assert.IsEmpty(await _factory.ManufacturerManager.ListAsync(x => true));
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

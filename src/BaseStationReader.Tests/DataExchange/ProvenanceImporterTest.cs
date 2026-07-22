using BaseStationReader.BusinessLogic.Database;
using BaseStationReader.BusinessLogic.Logging;
using BaseStationReader.Data;
using BaseStationReader.Interfaces.Database;
using BaseStationReader.Interfaces.DataExchange;
using BaseStationReader.Tests.Mocks;

namespace BaseStationReader.Tests.DataExchange
{
    [TestClass]
    public class ProvenanceImporterTest
    {
        private IDatabaseManagementFactory _factory;
        private IProvenanceImporter _importer;

        [TestInitialize]
        public void Initialise()
        {
            var context = BaseStationReaderDbContextFactory.CreateInMemoryDbContext();
            _factory = new DatabaseManagementFactory(new MockFileLogger(), context, 0);
            _importer = new ProvenanceImporter(_factory);
        }

        [TestMethod]
        public async Task ImportTestAsync()
        {
            await _importer.ImportAsync("provenance.csv");
            var records = await _factory.ProvenanceManager.ListAsync(x => true);

            Assert.HasCount(1, records);
            Assert.AreEqual("ref-1", records[0].SourceRef);
            Assert.AreEqual("Open data", records[0].Source);
            Assert.AreEqual("https://example.com/data", records[0].SourceUrl);
            Assert.AreEqual("Aircraft", records[0].SourceDataset);
            Assert.AreEqual("2026-07", records[0].SourceVersion);
            Assert.AreEqual("ODbL-1.0", records[0].Licence);
        }
    }
}

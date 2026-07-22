using BaseStationReader.BusinessLogic.Database;
using BaseStationReader.Data;
using BaseStationReader.Interfaces.Database;

namespace BaseStationReader.Tests.Database
{
    [TestClass]
    public class ProvenanceManagerTest
    {
        private IProvenanceManager _manager;

        [TestInitialize]
        public async Task InitialiseAsync()
        {
            BaseStationReaderDbContext context = BaseStationReaderDbContextFactory.CreateInMemoryDbContext();
            _manager = new ProvenanceManager(context);
            await _manager.AddAsync("ref-1", "Source", "https://example.com", "Dataset", "1.0", "MIT");
        }

        [TestMethod]
        public async Task AddDuplicateTestAsync()
        {
            await _manager.AddAsync("ref-1", "Other", "https://other.example", "Other", "2.0", "Other");
            Assert.HasCount(1, await _manager.ListAsync(x => true));
        }

        [TestMethod]
        public async Task AddAndGetTestAsync()
        {
            var record = await _manager.GetAsync(x => x.SourceRef == "ref-1");
            Assert.IsNotNull(record);
            Assert.AreEqual("Source", record.Source);
            Assert.AreEqual("https://example.com", record.SourceUrl);
            Assert.AreEqual("Dataset", record.SourceDataset);
            Assert.AreEqual("1.0", record.SourceVersion);
            Assert.AreEqual("MIT", record.Licence);
        }
    }
}

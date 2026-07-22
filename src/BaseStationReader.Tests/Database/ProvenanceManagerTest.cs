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

        [TestMethod]
        public async Task UpdateTestAsync()
        {
            var record = await _manager.GetAsync(x => x.SourceRef == "ref-1");
            await _manager.UpdateAsync(record.Id, "ref-2", "Updated source", "https://updated.example",
                "Updated dataset", "2.0", "ODbL");

            var updated = await _manager.GetAsync(x => x.Id == record.Id);
            Assert.AreEqual("ref-2", updated.SourceRef);
            Assert.AreEqual("Updated source", updated.Source);
            Assert.AreEqual("https://updated.example", updated.SourceUrl);
            Assert.AreEqual("Updated dataset", updated.SourceDataset);
            Assert.AreEqual("2.0", updated.SourceVersion);
            Assert.AreEqual("ODbL", updated.Licence);
        }

        [TestMethod]
        public async Task UpdateMissingTestAsync()
        {
            await Assert.ThrowsExactlyAsync<InvalidOperationException>(() =>
                _manager.UpdateAsync(999, "missing", "Source", "URL", "Dataset", "1", "MIT"));
        }

        [TestMethod]
        public async Task DeleteTestAsync()
        {
            var record = await _manager.GetAsync(x => x.SourceRef == "ref-1");
            await _manager.DeleteAsync(record.Id);
            Assert.IsEmpty(await _manager.ListAsync(x => true));
        }
    }
}

using BaseStationReader.BusinessLogic.Database;
using BaseStationReader.Data;
using BaseStationReader.Entities.Api;
using BaseStationReader.Interfaces.Database;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace BaseStationReader.Tests.Database
{
    [TestClass]
    public class AirlineCallsignPrefixManagerTest
    {
        private BaseStationReaderDbContext _context = null!;
        private IAirlineCallsignPrefixManager _manager = null!;
        private Airline _airline = null!;
        private Provenance _provenance = null!;

        [TestInitialize]
        public async Task InitialiseAsync()
        {
            _context = BaseStationReaderDbContextFactory.CreateInMemoryDbContext();
            var airlineManager = new AirlineManager(_context);
            var provenanceManager = new ProvenanceManager(_context);
            _provenance = await provenanceManager.AddAsync(
                "TEST", "Test source", "N/A", "Prefixes", "1", "N/A");
            _airline = await airlineManager.AddAsync("BA", "BAW", "British Airways", _provenance.Id);
            _manager = new AirlineCallsignPrefixManager(_context);
        }

        [TestCleanup]
        public async Task CleanupAsync()
            => await _context.DisposeAsync();

        [TestMethod]
        public async Task AddAndGetTestAsync()
        {
            var added = await _manager.AddAsync("  baw ", _airline.Id, _provenance.Id);
            var mapping = await _manager.GetAsync(x => x.Id == added.Id);

            Assert.IsNotNull(mapping);
            Assert.AreEqual("BAW", mapping.Prefix);
            Assert.AreEqual(_airline.Id, mapping.AirlineId);
            Assert.AreEqual("British Airways", mapping.Airline.Name);
            Assert.AreEqual("TEST", mapping.Provenance.SourceRef);
        }

        [TestMethod]
        public async Task AddUsesManualProvenanceTestAsync()
        {
            var mapping = await _manager.AddAsync("BAW", _airline.Id);

            Assert.AreEqual("MANUAL", mapping.Provenance.SourceRef);
        }

        [TestMethod]
        public async Task AddDuplicateIsIdempotentTestAsync()
        {
            var first = await _manager.AddAsync("BAW", _airline.Id, _provenance.Id);
            var second = await _manager.AddAsync("baw", _airline.Id, _provenance.Id);

            Assert.AreEqual(first.Id, second.Id);
            Assert.HasCount(1, await _manager.ListAsync(x => true));
        }

        [TestMethod]
        public async Task AddConflictingDuplicateThrowsTestAsync()
        {
            await _manager.AddAsync("BAW", _airline.Id, _provenance.Id);
            var otherProvenance = await new ProvenanceManager(_context).AddAsync(
                "OTHER", "Other source", "N/A", "Prefixes", "1", "N/A");

            await Assert.ThrowsExactlyAsync<InvalidOperationException>(() =>
                _manager.AddAsync("BAW", _airline.Id, otherProvenance.Id));
        }

        [TestMethod]
        [DataRow("")]
        [DataRow("   ")]
        [DataRow("TOO-LONG1")]
        [DataRow("BA*")]
        [DataRow("BA W")]
        public async Task AddInvalidPrefixThrowsTestAsync(string prefix)
        {
            await Assert.ThrowsExactlyAsync<ArgumentException>(() =>
                _manager.AddAsync(prefix, _airline.Id, _provenance.Id));
        }

        [TestMethod]
        public async Task AddMissingRelationshipsThrowsTestAsync()
        {
            await Assert.ThrowsExactlyAsync<InvalidOperationException>(() =>
                _manager.AddAsync("BAW", 999, _provenance.Id));
            await Assert.ThrowsExactlyAsync<InvalidOperationException>(() =>
                _manager.AddAsync("BAW", _airline.Id, 999));
        }

        [TestMethod]
        public async Task UpdateTestAsync()
        {
            var mapping = await _manager.AddAsync("BAW", _airline.Id, _provenance.Id);
            var otherAirline = await new AirlineManager(_context).AddAsync(
                "VS", "VIR", "Virgin Atlantic", _provenance.Id);
            var updated = await _manager.UpdateAsync(
                mapping.Id, " vir ", otherAirline.Id, _provenance.Id);

            Assert.AreEqual("VIR", updated.Prefix);
            Assert.AreEqual("VIR", updated.Airline.ICAO);
        }

        [TestMethod]
        public async Task UpdateDuplicatePrefixThrowsTestAsync()
        {
            var first = await _manager.AddAsync("BAW", _airline.Id, _provenance.Id);
            var second = await _manager.AddAsync("SHT", _airline.Id, _provenance.Id);

            await Assert.ThrowsExactlyAsync<InvalidOperationException>(() =>
                _manager.UpdateAsync(second.Id, first.Prefix, _airline.Id, _provenance.Id));
        }

        [TestMethod]
        public async Task DeleteTestAsync()
        {
            var mapping = await _manager.AddAsync("BAW", _airline.Id, _provenance.Id);
            await _manager.DeleteAsync(mapping.Id);

            Assert.IsEmpty(await _manager.ListAsync(x => true));
        }

        [TestMethod]
        public void FactoryExposesManagerTest()
        {
            var factory = new DatabaseManagementFactory(
                new Mocks.MockFileLogger(), _context, 0);

            Assert.IsNotNull(factory.AirlineCallsignPrefixManager);
        }

        [TestMethod]
        public async Task ReferencedAirlineAndProvenanceCannotBeDeletedTestAsync()
        {
            await using var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var options = new DbContextOptionsBuilder<BaseStationReaderDbContext>()
                .UseSqlite(connection)
                .Options;
            await using var context = new BaseStationReaderDbContext(options);
            await context.Database.EnsureCreatedAsync();
            var provenance = new Provenance
            {
                SourceRef = "TEST",
                Source = "Test source",
                SourceUrl = "N/A",
                SourceDataset = "Prefixes",
                SourceVersion = "1",
                Licence = "N/A"
            };
            context.Provenance.Add(provenance);
            await context.SaveChangesAsync();
            var airline = await new AirlineManager(context).AddAsync(
                "BA", "BAW", "British Airways", provenance.Id);
            await new AirlineCallsignPrefixManager(context).AddAsync(
                "BAW", airline.Id, provenance.Id);

            await Assert.ThrowsExactlyAsync<SqliteException>(() =>
                context.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM AIRLINE WHERE Id = {airline.Id}"));
            await Assert.ThrowsExactlyAsync<SqliteException>(() =>
                context.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM PROVENANCE WHERE Id = {provenance.Id}"));
        }
    }
}

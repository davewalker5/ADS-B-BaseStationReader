using BaseStationReader.BusinessLogic.Database;
using BaseStationReader.BusinessLogic.Import;
using BaseStationReader.Data;
using BaseStationReader.Entities.Api;
using BaseStationReader.Interfaces.Database;
using BaseStationReader.Interfaces.DataExchange;
using BaseStationReader.Tests.Mocks;

namespace BaseStationReader.Tests.DataExchange
{
    [TestClass]
    public class AirlineCallsignPrefixImporterTest
    {
        private BaseStationReaderDbContext _context = null!;
        private IDatabaseManagementFactory _factory = null!;
        private IAirlineCallsignPrefixImporter _importer = null!;
        private Airline _airline = null!;
        private Provenance _provenance = null!;

        [TestInitialize]
        public async Task InitialiseAsync()
        {
            _context = BaseStationReaderDbContextFactory.CreateInMemoryDbContext();
            _factory = new DatabaseManagementFactory(new MockFileLogger(), _context, 0);
            _provenance = await _factory.ProvenanceManager.AddAsync(
                "TEST", "Test source", "N/A", "Prefixes", "1", "N/A");
            _airline = await _factory.AirlineManager.AddAsync(
                "BA", "BAW", "British Airways", _provenance.Id);
            _importer = new AirlineCallsignPrefixImporter(_factory);
        }

        [TestCleanup]
        public async Task CleanupAsync()
            => await _context.DisposeAsync();

        [TestMethod]
        public async Task ImportTestAsync()
        {
            await _importer.ImportAsync("airline-callsign-prefixes.csv");
            var mappings = await _factory.AirlineCallsignPrefixManager.ListAsync(x => true);

            Assert.HasCount(2, mappings);
            Assert.IsTrue(mappings.Any(x => x.Prefix == "BAW"));
            Assert.IsTrue(mappings.All(x => x.Airline.ICAO == "BAW"));
            Assert.IsTrue(mappings.All(x => x.Provenance.SourceRef == "TEST"));
        }

        [TestMethod]
        public async Task ReimportIsIdempotentTestAsync()
        {
            await _importer.ImportAsync("airline-callsign-prefixes.csv");
            await _importer.ImportAsync("airline-callsign-prefixes.csv");

            Assert.HasCount(2, await _factory.AirlineCallsignPrefixManager.ListAsync(x => true));
        }

        [TestMethod]
        public async Task EmptyAndMissingFilesDoNotSaveTestAsync()
        {
            await _importer.ImportAsync("empty_airline-callsign-prefixes.csv");
            await _importer.ImportAsync("missing-prefixes.csv");

            Assert.IsEmpty(await _factory.AirlineCallsignPrefixManager.ListAsync(x => true));
        }

        [TestMethod]
        public async Task MissingAirlineFailsBeforeSavingTestAsync()
        {
            var mappings = new[]
            {
                Mapping("BAW", "BAW", "TEST"),
                Mapping("ZZZ", "ZZZ", "TEST")
            };

            var exception = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() =>
                _importer.SaveAsync(mappings));

            StringAssert.Contains(exception.Message, "ZZZ");
            Assert.IsEmpty(await _factory.AirlineCallsignPrefixManager.ListAsync(x => true));
        }

        [TestMethod]
        public async Task MissingProvenanceFailsBeforeSavingTestAsync()
        {
            var exception = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() =>
                _importer.SaveAsync([Mapping("BAW", "BAW", "MISSING")]));

            StringAssert.Contains(exception.Message, "MISSING");
            Assert.IsEmpty(await _factory.AirlineCallsignPrefixManager.ListAsync(x => true));
        }

        [TestMethod]
        public async Task AmbiguousAirlineFailsBeforeSavingTestAsync()
        {
            _context.Airlines.Add(new Airline
            {
                ICAO = "BAW", IATA = "BX", Name = "Duplicate", ProvenanceId = _provenance.Id
            });
            await _context.SaveChangesAsync();

            var exception = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() =>
                _importer.SaveAsync([Mapping("BAW", "BAW", "TEST")]));

            StringAssert.Contains(exception.Message, "ambiguous airline ICAO");
            Assert.IsEmpty(await _factory.AirlineCallsignPrefixManager.ListAsync(x => true));
        }

        [TestMethod]
        public async Task IdenticalDuplicateRowsAreCollapsedTestAsync()
        {
            await _importer.SaveAsync([
                Mapping(" baw ", "baw", "TEST"),
                Mapping("BAW", "BAW", "TEST")]);

            Assert.HasCount(1, await _factory.AirlineCallsignPrefixManager.ListAsync(x => true));
        }

        [TestMethod]
        public async Task ConflictingDuplicateRowsFailBeforeSavingTestAsync()
        {
            var other = await _factory.AirlineManager.AddAsync(
                "VS", "VIR", "Virgin Atlantic", _provenance.Id);
            Assert.IsNotNull(other);

            var exception = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() =>
                _importer.SaveAsync([
                    Mapping("ABC", "BAW", "TEST"),
                    Mapping("abc", "VIR", "TEST")]));

            StringAssert.Contains(exception.Message, "ABC");
            Assert.IsEmpty(await _factory.AirlineCallsignPrefixManager.ListAsync(x => true));
        }

        [TestMethod]
        public async Task ExistingMappingConflictFailsBeforeSavingTestAsync()
        {
            await _factory.AirlineCallsignPrefixManager.AddAsync(
                "BAW", _airline.Id, _provenance.Id);
            var other = await _factory.AirlineManager.AddAsync(
                "VS", "VIR", "Virgin Atlantic", _provenance.Id);
            Assert.IsNotNull(other);

            var exception = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() =>
                _importer.SaveAsync([
                    Mapping("SHT", "BAW", "TEST"),
                    Mapping("BAW", "VIR", "TEST")]));

            StringAssert.Contains(exception.Message, "BAW");
            var mappings = await _factory.AirlineCallsignPrefixManager.ListAsync(x => true);
            Assert.HasCount(1, mappings);
            Assert.AreEqual("BAW", mappings[0].Prefix);
            Assert.AreEqual(_airline.Id, mappings[0].AirlineId);
        }

        [TestMethod]
        public async Task InvalidPrefixFailsBeforeSavingTestAsync()
        {
            await Assert.ThrowsExactlyAsync<ArgumentException>(() =>
                _importer.SaveAsync([
                    Mapping("BAW", "BAW", "TEST"),
                    Mapping("BAD*", "BAW", "TEST")]));

            Assert.IsEmpty(await _factory.AirlineCallsignPrefixManager.ListAsync(x => true));
        }

        private static AirlineCallsignPrefix Mapping(
            string prefix,
            string airlineIcao,
            string provenance)
            => new()
            {
                Prefix = prefix,
                AirlineIcaoRef = airlineIcao,
                ProvenanceRef = provenance
            };
    }
}

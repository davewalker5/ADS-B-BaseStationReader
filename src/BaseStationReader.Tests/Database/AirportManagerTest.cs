using BaseStationReader.BusinessLogic.Database;
using BaseStationReader.Data;
using BaseStationReader.Entities.Api;
using BaseStationReader.Interfaces.Database;

namespace BaseStationReader.Tests.Database
{
    [TestClass]
    public class AirportManagerTest
    {
        private const string ICAO = "EGLL";
        private const string IATA = "LHR";
        private const string Name = "London Heathrow Airport";
        private int _provenanceId;

        private IAirportManager _manager = null;

        /// <summary>
        /// Create a manager and seed one airport before each test.
        /// </summary>
        [TestInitialize]
        public async Task InitialiseAsync()
        {
            var context = BaseStationReaderDbContextFactory.CreateInMemoryDbContext();
            _provenanceId = (await new ProvenanceManager(context)
                .AddAsync("MANUAL", "N/A", "N/A", "N/A", "N/A", "N/A")).Id;
            _manager = new AirportManager(context);
            _ = await _manager.AddAsync(CreateAirport());
        }

        /// <summary>
        /// Verify duplicate airports are not inserted.
        /// </summary>
        [TestMethod]
        public async Task AddDuplicateTestAsync()
        {
            await _manager.AddAsync(CreateAirport());
            var airports = await _manager.ListAsync(x => true);

            Assert.HasCount(1, airports);
            AssertAirport(airports[0]);
        }

        /// <summary>
        /// Verify an airport can be retrieved using a predicate.
        /// </summary>
        [TestMethod]
        public async Task GetTestAsync()
        {
            var airport = await _manager.GetAsync(x => x.Name == Name);

            Assert.IsNotNull(airport);
            Assert.IsGreaterThan(0, airport.Id);
            AssertAirport(airport);
        }

        /// <summary>
        /// Verify airports can be retrieved by ICAO, IATA and name.
        /// </summary>
        [TestMethod]
        [DataRow(null, ICAO, null)]
        [DataRow(IATA, null, null)]
        [DataRow(null, null, Name)]
        public async Task GetByIdentityTestAsync(string iata, string icao, string name)
        {
            var airport = await _manager.GetAsync(iata, icao, name);

            Assert.IsNotNull(airport);
            AssertAirport(airport);
        }

        /// <summary>
        /// Verify a missing airport returns null.
        /// </summary>
        [TestMethod]
        public async Task GetMissingTestAsync()
            => Assert.IsNull(await _manager.GetAsync(x => x.Name == "Missing"));

        /// <summary>
        /// Verify list queries return the expected airport.
        /// </summary>
        [TestMethod]
        public async Task ListAllTestAsync()
        {
            var airports = await _manager.ListAsync(x => true);
            Assert.HasCount(1, airports);
            AssertAirport(airports[0]);
        }

        /// <summary>
        /// Verify list queries return an empty collection when no airport matches.
        /// </summary>
        [TestMethod]
        public async Task ListMissingTestAsync()
            => Assert.IsEmpty(await _manager.ListAsync(x => x.Name == "Missing"));

        [TestMethod]
        public async Task UpdateTestAsync()
        {
            var airport = await _manager.GetAsync(x => x.ICAO == ICAO);
            airport.IATA = "LGW";
            airport.ICAO = "EGKK";
            airport.Name = "London Gatwick Airport";
            airport.Latitude = 51.1537;
            airport.Longitude = -0.1821;

            var updated = await _manager.UpdateAsync(airport);

            Assert.AreEqual("LGW", updated.IATA);
            Assert.AreEqual("EGKK", updated.ICAO);
            Assert.AreEqual("London Gatwick Airport", updated.Name);
            Assert.AreEqual(51.1537, updated.Latitude);
            Assert.AreEqual(-0.1821, updated.Longitude);
        }

        [TestMethod]
        public async Task DeleteTestAsync()
        {
            var airport = await _manager.GetAsync(x => x.ICAO == ICAO);
            await _manager.DeleteAsync(airport.Id);
            Assert.IsEmpty(await _manager.ListAsync(x => true));
        }

        /// <summary>
        /// Create an airport matching the import format.
        /// </summary>
        private Airport CreateAirport() => new()
        {
            ICAO = ICAO,
            IATA = IATA,
            Name = Name,
            Latitude = 51.4706,
            Longitude = -0.461941,
            ProvenanceId = _provenanceId
        };

        /// <summary>
        /// Assert that all airport details were persisted.
        /// </summary>
        private void AssertAirport(Airport airport)
        {
            Assert.AreEqual(IATA, airport.IATA);
            Assert.AreEqual(ICAO, airport.ICAO);
            Assert.AreEqual(Name, airport.Name);
            Assert.AreEqual(51.4706, airport.Latitude);
            Assert.AreEqual(-0.461941, airport.Longitude);
            Assert.AreEqual(_provenanceId, airport.ProvenanceId);
            Assert.AreEqual("MANUAL", airport.Provenance.SourceRef);
        }
    }
}

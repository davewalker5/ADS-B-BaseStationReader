using BaseStationReader.Data;
using BaseStationReader.BusinessLogic.Database;
using BaseStationReader.Interfaces.Database;

namespace BaseStationReader.Tests.Database
{
    [TestClass]
    public class AirlineManagerTest
    {
        private const string ICAO = "BAW";
        private const string IATA = "BA";
        private const string Name = "British Airways";

        private IAirlineManager _manager = null;

        [TestInitialize]
        public async Task InitialiseAsync()
        {
            BaseStationReaderDbContext context = BaseStationReaderDbContextFactory.CreateInMemoryDbContext();
            _manager = new AirlineManager(context);
            _ = await _manager.AddAsync(IATA, ICAO, Name);
        }

        [TestMethod]
        public async Task AddDuplicateTestAsync()
        {
            await _manager.AddAsync(IATA, ICAO, Name);
            var airlines = await _manager.ListAsync(x => true);
            Assert.HasCount(1, airlines);
            Assert.AreEqual(IATA, airlines[0].IATA);
            Assert.AreEqual(ICAO, airlines[0].ICAO);
            Assert.AreEqual(Name, airlines[0].Name);
        }

        [TestMethod]
        public async Task GetTestAsync()
        {
            var airline = await _manager.GetAsync(a => a.Name == Name);
            Assert.IsNotNull(airline);
            Assert.IsGreaterThan(0, airline.Id);
            Assert.AreEqual(IATA, airline.IATA);
            Assert.AreEqual(ICAO, airline.ICAO);
            Assert.AreEqual(Name, airline.Name);
        }

        [TestMethod]
        public async Task GetByICAOTestAsync()
        {
            var airline = await _manager.GetAsync(null, ICAO, null);
            Assert.IsNotNull(airline);
            Assert.IsGreaterThan(0, airline.Id);
            Assert.AreEqual(IATA, airline.IATA);
            Assert.AreEqual(ICAO, airline.ICAO);
            Assert.AreEqual(Name, airline.Name);
        }

        [TestMethod]
        public async Task GetByIATATestAsync()
        {
            var airline = await _manager.GetAsync(IATA, null, null);
            Assert.IsNotNull(airline);
            Assert.IsGreaterThan(0, airline.Id);
            Assert.AreEqual(IATA, airline.IATA);
            Assert.AreEqual(ICAO, airline.ICAO);
            Assert.AreEqual(Name, airline.Name);
        }

        [TestMethod]
        public async Task GetByNameTestAsync()
        {
            var airline = await _manager.GetAsync(null, null, Name);
            Assert.IsNotNull(airline);
            Assert.IsGreaterThan(0, airline.Id);
            Assert.AreEqual(IATA, airline.IATA);
            Assert.AreEqual(ICAO, airline.ICAO);
            Assert.AreEqual(Name, airline.Name);
        }

        [TestMethod]
        public async Task GetMissingTestAsync()
        {
            var airline = await _manager.GetAsync(a => a.Name == "Missing");
            Assert.IsNull(airline);
        }

        [TestMethod]
        public async Task ListAllTestAsync()
        {
            var airlines = await _manager.ListAsync(x => true);
            Assert.HasCount(1, airlines);
            Assert.AreEqual(Name, airlines.First().Name);
        }

        [TestMethod]
        public async Task ListMissingTestAsync()
        {
            var airlines = await _manager.ListAsync(e => e.Name == "Missing");
            Assert.IsEmpty(airlines);
        }
    }
}

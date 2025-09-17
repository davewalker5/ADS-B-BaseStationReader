using BaseStationReader.Data;
using BaseStationReader.Entities.Interfaces;
using BaseStationReader.BusinessLogic.Database;

namespace BaseStationReader.Tests
{
    [TestClass]
    public class AirlineManagerTest
    {
        private const string IATA = "BA";
        private const string ICAO = "BAW";
        private const string Name = "British Airways";

        private IAirlineManager _manager = null;

        [TestInitialize]
        public void TestInitialize()
        {
            BaseStationReaderDbContext context = BaseStationReaderDbContextFactory.CreateInMemoryDbContext();
            _manager = new AirlineManager(context);
            Task.Run(() => _manager.AddAsync(IATA, ICAO, Name)).Wait();
        }

        [TestMethod]
        public async Task AddDuplicateByIATATest()
        {
            await _manager!.AddAsync(IATA, "XX", "Some Other Name");
            var airlines = await _manager.ListAsync(x => true);
            Assert.AreEqual(1, airlines.Count);
        }

        [TestMethod]
        public async Task AddDuplicateByICAOTest()
        {
            await _manager!.AddAsync("XX", ICAO, "Some Other Name");
            var airlines = await _manager.ListAsync(x => true);
            Assert.AreEqual(1, airlines.Count);
        }

        [TestMethod]
        public async Task AddDuplicateByNameTest()
        {
            await _manager!.AddAsync("XX", "XX", Name);
            var airlines = await _manager.ListAsync(x => true);
            Assert.AreEqual(1, airlines.Count);
        }

        [TestMethod]
        public async Task AddAndGetByIATATest()
        {
            var airline = await _manager!.GetAsync(a => a.IATA == IATA);
            Assert.IsNotNull(airline);
            Assert.IsTrue(airline.Id > 0);
            Assert.AreEqual(IATA, airline.IATA);
            Assert.AreEqual(ICAO, airline.ICAO);
            Assert.AreEqual(Name, airline.Name);
        }

        [TestMethod]
        public async Task AddAndGetByICAOTest()
        {
            var airline = await _manager!.GetAsync(a => a.ICAO == ICAO);
            Assert.IsNotNull(airline);
            Assert.IsTrue(airline.Id > 0);
            Assert.AreEqual(IATA, airline.IATA);
            Assert.AreEqual(ICAO, airline.ICAO);
            Assert.AreEqual(Name, airline.Name);
        }

        [TestMethod]
        public async Task AddAndGetByNameTest()
        {
            var airline = await _manager!.GetAsync(a => a.Name == Name);
            Assert.IsNotNull(airline);
            Assert.IsTrue(airline.Id > 0);
            Assert.AreEqual(IATA, airline.IATA);
            Assert.AreEqual(ICAO, airline.ICAO);
            Assert.AreEqual(Name, airline.Name);
        }

        [TestMethod]
        public async Task GetMissingTest()
        {
            var airline = await _manager!.GetAsync(a => a.Name == "Missing");
            Assert.IsNull(airline);
        }

        [TestMethod]
        public async Task ListAllTest()
        {
            var airlines = await _manager!.ListAsync(x => true);
            Assert.AreEqual(1, airlines!.Count);
            Assert.AreEqual(IATA, airlines.First().IATA);
            Assert.AreEqual(ICAO, airlines.First().ICAO);
            Assert.AreEqual(Name, airlines.First().Name);
        }

        [TestMethod]
        public async Task ListMissingTest()
        {
            var airlines = await _manager!.ListAsync(e => e.Name == "Missing");
            Assert.AreEqual(0, airlines!.Count);
        }
    }
}

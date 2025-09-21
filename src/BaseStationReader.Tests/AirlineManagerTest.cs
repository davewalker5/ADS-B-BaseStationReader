using BaseStationReader.Data;
using BaseStationReader.Entities.Interfaces;
using BaseStationReader.BusinessLogic.Database;

namespace BaseStationReader.Tests
{
    [TestClass]
    public class AirlineManagerTest
    {
        private const string ICAO = "BAW";
        private const string IATA = "BA";
        private const string Name = "British Airways";

        private IAirlineManager _manager = null;

        [TestInitialize]
        public async Task TestInitialize()
        {
            BaseStationReaderDbContext context = BaseStationReaderDbContextFactory.CreateInMemoryDbContext();
            _manager = new AirlineManager(context);
            _ = await _manager.AddAsync(IATA, ICAO, Name);
        }

        [TestMethod]
        public async Task AddDuplicateTest()
        {
            await _manager.AddAsync(IATA, ICAO, Name);
            var airlines = await _manager.ListAsync(x => true);
            Assert.HasCount(1, airlines);
        }

        [TestMethod]
        public async Task AddAndGetTest()
        {
            var airline = await _manager.GetAsync(a => a.Name == Name);
            Assert.IsNotNull(airline);
            Assert.IsGreaterThan(0, airline.Id);
            Assert.AreEqual(Name, airline.Name);
        }

        [TestMethod]
        public async Task GetMissingTest()
        {
            var airline = await _manager.GetAsync(a => a.Name == "Missing");
            Assert.IsNull(airline);
        }

        [TestMethod]
        public async Task ListAllTest()
        {
            var airlines = await _manager.ListAsync(x => true);
            Assert.HasCount(1, airlines);
            Assert.AreEqual(Name, airlines.First().Name);
        }

        [TestMethod]
        public async Task ListMissingTest()
        {
            var airlines = await _manager.ListAsync(e => e.Name == "Missing");
            Assert.IsEmpty(airlines);
        }
    }
}

using BaseStationReader.Data;
using BaseStationReader.Entities.Interfaces;
using BaseStationReader.BusinessLogic.Database;

namespace BaseStationReader.Tests
{
    [TestClass]
    public class ModelManagerTest
    {
        private IModelManager _manager = null;

        [TestInitialize]
        public void Initialise()
        {
            BaseStationReaderDbContext context = BaseStationReaderDbContextFactory.CreateInMemoryDbContext();

            // Set up a manufacturer
            var manufacturerManager = new ManufacturerManager(context);
            var manufacturerId = Task.Run(() => manufacturerManager.AddAsync("Airbus")).Result.Id;

            // Add two aircraft models
            _manager = new ModelManager(context);
            Task.Run(() => _manager.AddAsync("332", "A332", "A330-200", manufacturerId)).Wait();
            Task.Run(() => _manager.AddAsync("345", "A345", "A340-500", manufacturerId)).Wait();
        }

        [TestMethod]
        public void GetAircraftByIATATest()
        {
            var aircraft = Task.Run(() => _manager!.GetAsync(x => x.IATA == "332")).Result;
            Assert.AreEqual("332", aircraft.IATA);
            Assert.AreEqual("A332", aircraft.ICAO);
            Assert.AreEqual("A330-200", aircraft.Name);
            Assert.AreEqual("Airbus", aircraft.Manufacturer.Name);
        }

        [TestMethod]
        public void GetAircraftByICAOTest()
        {
            var aircraft = Task.Run(() => _manager!.GetAsync(x => x.ICAO == "A345")).Result;
            Assert.AreEqual("345", aircraft.IATA);
            Assert.AreEqual("A345", aircraft.ICAO);
            Assert.AreEqual("A340-500", aircraft.Name);
            Assert.AreEqual("Airbus", aircraft.Manufacturer.Name);
        }

        [TestMethod]
        public void GetAircraftByNameTest()
        {
            var aircraft = Task.Run(() => _manager!.GetAsync(x => x.Name == "A330-200")).Result;
            Assert.AreEqual("332", aircraft.IATA);
            Assert.AreEqual("A332", aircraft.ICAO);
            Assert.AreEqual("A330-200", aircraft.Name);
            Assert.AreEqual("Airbus", aircraft.Manufacturer.Name);
        }

        [TestMethod]
        public void ListAircraftByManufacturerTest()
        {
            var aircraft = Task.Run(() => _manager!.ListAsync(x => x.Manufacturer.Name == "Airbus")).Result;
            Assert.AreEqual(2, aircraft.Count);
            Assert.IsNotNull(aircraft.Find(x => x.IATA == "332"));
            Assert.IsNotNull(aircraft.Find(x => x.IATA == "345"));
        }
    }
}

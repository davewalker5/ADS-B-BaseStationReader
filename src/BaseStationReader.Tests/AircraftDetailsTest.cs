using BaseStationReader.Data;
using BaseStationReader.Entities.Interfaces;
using BaseStationReader.Logic.Database;
using DocumentFormat.OpenXml.ExtendedProperties;

namespace BaseStationReader.Tests
{
    [TestClass]
    public class AircraftDetailsTest
    {
        private readonly string Address = new Random().Next(0, 16777215).ToString("X6");
        private const string Manufacturer = "Airbus";
        private const string AirlineIATA = "BA";
        private const string AirlineICAO = "BAW";
        private const string Airline = "British Airways";
        private const string ModelIATA = "332";
        private const string ModelICAO = "A332";
        private const string ModelName = "A330-200";

        private IAircraftDetailsManager _manager = null;

        [TestInitialize]
        public void Initialise()
        {
            BaseStationReaderDbContext context = BaseStationReaderDbContextFactory.CreateInMemoryDbContext();

            // Set up a manufacturer
            var manufacturerManager = new ManufacturerManager(context);
            var manufacturerId = Task.Run(() => manufacturerManager.AddAsync(Manufacturer)).Result.Id;

            // Set up an airline
            var airlineManager = new AirlineManager(context);
            var airlineId = Task.Run(() => airlineManager.AddAsync(AirlineIATA, AirlineICAO, Airline)).Result.Id;

            // Add an aircraft model
            var modelManager = new ModelManager(context);
            var modelId = Task.Run(() => modelManager.AddAsync(ModelIATA, ModelICAO, ModelName, manufacturerId)).Result.Id;

            // Set up a details record
            _manager = new AircraftDetailsManager(context);
            Task.Run(() => _manager.AddAsync(Address, airlineId, modelId)).Wait();
        }

        [TestMethod]
        public async Task AddDuplicateTest()
        {
            await _manager!.AddAsync(Address, null, null);
            var details = await _manager.ListAsync(x => true);
            Assert.AreEqual(1, details.Count);
        }

        [TestMethod]
        public async Task AddAndGetTest()
        {
            var details = await _manager!.GetAsync(a => a.Address == Address);
            Assert.IsNotNull(details);
            Assert.IsTrue(details.Id > 0);
            Assert.AreEqual(Address, details.Address);
            Assert.AreEqual(Airline, details.Airline!.Name);
            Assert.AreEqual(AirlineIATA, details.Airline!.IATA);
            Assert.AreEqual(AirlineICAO, details.Airline!.ICAO);
            Assert.AreEqual(ModelIATA, details.Model!.IATA);
            Assert.AreEqual(ModelICAO, details.Model!.ICAO);
            Assert.AreEqual(ModelName, details.Model!.Name);
            Assert.AreEqual(Manufacturer, details.Model!.Manufacturer.Name);
        }
    }
}


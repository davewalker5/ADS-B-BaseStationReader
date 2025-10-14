using BaseStationReader.BusinessLogic.Database;
using BaseStationReader.BusinessLogic.Logging;
using BaseStationReader.Data;
using BaseStationReader.Interfaces.DataExchange;
using BaseStationReader.Interfaces.Database;
using BaseStationReader.Tests.Mocks;

namespace BaseStationReader.Tests.DataExchange
{
    [TestClass]
    public class AircraftImporterTest
    {
        private const string ManufacturerName = "Airbus";
        private const string ModelICAO = "A20N";
        private const string ModelIATA = "32N";
        private const string ModelName = "Airbus A320neo";

        private IManufacturerManager _manufacturerManager;
        private IModelManager _modelManager;
        private IAircraftManager _aircraftManager;
        private IAircraftImporter _importer;

        [TestInitialize]
        public void Initialise()
        {
            var context = BaseStationReaderDbContextFactory.CreateInMemoryDbContext();
            var logger = new MockFileLogger();
            _manufacturerManager = new ManufacturerManager(context);
            _modelManager = new ModelManager(context);
            _aircraftManager = new AircraftManager(context);
            _importer = new AircraftImporter(_aircraftManager, _modelManager, logger);
        }

        [TestMethod]
        public async Task ImportTestAsync()
        {
            var manufacturer = await _manufacturerManager.AddAsync(ManufacturerName);
            _ = await _modelManager.AddAsync(ModelIATA, ModelICAO, ModelName, manufacturer.Id);

            await _importer.ImportAsync("aircraft.csv");
            var aircraft = await _aircraftManager.ListAsync(x => true);

            Assert.IsNotNull(aircraft);
            Assert.HasCount(1, aircraft);
            Assert.IsGreaterThan(0, aircraft[0].Id);
            Assert.AreEqual("45C86B", aircraft[0].Address);
            Assert.AreEqual("OY-RCK", aircraft[0].Registration);
            Assert.IsNull(aircraft[0].Manufactured);
            Assert.IsNull(aircraft[0].Age);
            Assert.AreEqual(ModelICAO, aircraft[0].Model.ICAO);
            Assert.AreEqual(ModelICAO, aircraft[0].Model.ICAO);
            Assert.AreEqual(ModelIATA, aircraft[0].Model.IATA);
            Assert.AreEqual(ModelName, aircraft[0].Model.Name);
            Assert.AreEqual(ManufacturerName, aircraft[0].Model.Manufacturer.Name);
        }

        [TestMethod]
        public async Task ImportWithoutModelPresentTestAsync()
        {
            await _importer.ImportAsync("aircraft.csv");
            var aircraft = await _modelManager.ListAsync(x => true);

            Assert.IsNotNull(aircraft);
            Assert.HasCount(0, aircraft);
        }

        [TestMethod]
        public async Task ImportEmptyFileTestAsync()
        {
            await _importer.ImportAsync("empty_aircraft.csv");
            var aircraft = await _modelManager.ListAsync(x => true);

            Assert.IsNotNull(aircraft);
            Assert.HasCount(0, aircraft);
        }

        [TestMethod]
        public async Task ImportMissingFileTestAsync()
        {
            await _importer.ImportAsync("missing.csv");
            var aircraft = await _modelManager.ListAsync(x => true);

            Assert.IsNotNull(aircraft);
            Assert.HasCount(0, aircraft);
        }
    }
}
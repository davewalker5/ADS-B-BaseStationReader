using BaseStationReader.Data;
using BaseStationReader.Entities.Interfaces;
using BaseStationReader.BusinessLogic.Database;
using BaseStationReader.Entities.Lookup;

namespace BaseStationReader.Tests.Database
{
    [TestClass]
    public class ModelManagerTest
    {
        private const string Manufacturer = "Airbus";
        private const string ModelIATA = "332";
        private const string ModelICAO = "A332";
        private const string ModelName = "A330-200";

        private IModelManager _manager = null;
        private Manufacturer _manufacturer;

        [TestInitialize]
        public async Task Initialise()
        {
            // Create a context and a model management class to test
            BaseStationReaderDbContext context = BaseStationReaderDbContextFactory.CreateInMemoryDbContext();
            _manager = new ModelManager(context);

            // Set up a manufacturer and an aircraft model
            _manufacturer = await new ManufacturerManager(context).AddAsync(Manufacturer);
            _ = await _manager.AddAsync(ModelIATA, ModelICAO, ModelName, _manufacturer.Id);
        }

        [TestMethod]
        public async Task AddDuplicateTest()
        {
            await _manager.AddAsync(ModelIATA, ModelICAO, ModelName, _manufacturer.Id);
            var models = await _manager.ListAsync(x => true);
            Assert.HasCount(1, models);
        }

        [TestMethod]
        public async Task AddAndGetTest()
        {
            var model = await _manager.GetAsync(a => a.IATA == ModelIATA);
            Assert.IsNotNull(model);
            Assert.IsGreaterThan(0, model.Id);
            Assert.AreEqual(ModelIATA, model.IATA);
            Assert.AreEqual(ModelICAO, model.ICAO);
            Assert.AreEqual(ModelName, model.Name);
            Assert.AreEqual(Manufacturer, model.Manufacturer.Name);
        }

        [TestMethod]
        public async Task GetByICAOCodeTest()
        {
            var model = await _manager.GetByCodeAsync(null, ModelICAO);
            Assert.IsNotNull(model);
            Assert.IsGreaterThan(0, model.Id);
            Assert.AreEqual(ModelIATA, model.IATA);
            Assert.AreEqual(ModelICAO, model.ICAO);
            Assert.AreEqual(ModelName, model.Name);
            Assert.AreEqual(Manufacturer, model.Manufacturer.Name);
        }

        [TestMethod]
        public async Task GetByIATACodeTest()
        {
            var model = await _manager.GetByCodeAsync(ModelIATA, null);
            Assert.IsNotNull(model);
            Assert.IsGreaterThan(0, model.Id);
            Assert.AreEqual(ModelIATA, model.IATA);
            Assert.AreEqual(ModelICAO, model.ICAO);
            Assert.AreEqual(ModelName, model.Name);
            Assert.AreEqual(Manufacturer, model.Manufacturer.Name);
        }

        [TestMethod]
        public async Task GetMissingTest()
        {
            var model = await _manager.GetAsync(a => a.IATA == "Missing");
            Assert.IsNull(model);
        }

        [TestMethod]
        public async Task ListAllTest()
        {
            var models = await _manager.ListAsync(x => true);
            Assert.IsNotNull(models);
            Assert.HasCount(1, models);
            Assert.IsGreaterThan(0, models[0].Id);
            Assert.AreEqual(ModelIATA, models[0].IATA);
            Assert.AreEqual(ModelICAO, models[0].ICAO);
            Assert.AreEqual(ModelName, models[0].Name);
            Assert.AreEqual(Manufacturer, models[0].Manufacturer.Name);
        }

        [TestMethod]
        public async Task ListMissingTest()
        {
            var models = await _manager.ListAsync(x => x.IATA == "Missing");
            Assert.IsEmpty(models);
        }
    }
}


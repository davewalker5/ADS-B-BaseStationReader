using BaseStationReader.Data;
using BaseStationReader.BusinessLogic.Database;
using BaseStationReader.Entities.Api;
using BaseStationReader.Interfaces.Database;

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
        public async Task InitialiseAsync()
        {
            // Create a context and a model management class to test
            BaseStationReaderDbContext context = BaseStationReaderDbContextFactory.CreateInMemoryDbContext();
            _manager = new ModelManager(context);

            // Set up a manufacturer and an aircraft model
            _manufacturer = await new ManufacturerManager(context).AddAsync(Manufacturer);
            _ = await _manager.AddAsync(ModelIATA, ModelICAO, ModelName, _manufacturer.Id);
        }

        [TestMethod]
        public async Task AddDuplicateTestAsync()
        {
            await _manager.AddAsync(ModelIATA, ModelICAO, ModelName, _manufacturer.Id);
            var models = await _manager.ListAsync(x => true);
            Assert.HasCount(1, models);
        }

        [TestMethod]
        public async Task AddAndGetTestAsync()
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
        public async Task GetByICAOCodeTestAsync()
        {
            var model = await _manager.GetAsync(null, ModelICAO, null);
            Assert.IsNotNull(model);
            Assert.IsGreaterThan(0, model.Id);
            Assert.AreEqual(ModelIATA, model.IATA);
            Assert.AreEqual(ModelICAO, model.ICAO);
            Assert.AreEqual(ModelName, model.Name);
            Assert.AreEqual(Manufacturer, model.Manufacturer.Name);
        }

        [TestMethod]
        public async Task GetByIATACodeTestAsync()
        {
            var model = await _manager.GetAsync(ModelIATA, null, null);
            Assert.IsNotNull(model);
            Assert.IsGreaterThan(0, model.Id);
            Assert.AreEqual(ModelIATA, model.IATA);
            Assert.AreEqual(ModelICAO, model.ICAO);
            Assert.AreEqual(ModelName, model.Name);
            Assert.AreEqual(Manufacturer, model.Manufacturer.Name);
        }

        [TestMethod]
        public async Task GetByNameTestAsync()
        {
            var model = await _manager.GetAsync(null, null, ModelName);
            Assert.IsNotNull(model);
            Assert.IsGreaterThan(0, model.Id);
            Assert.AreEqual(ModelIATA, model.IATA);
            Assert.AreEqual(ModelICAO, model.ICAO);
            Assert.AreEqual(ModelName, model.Name);
            Assert.AreEqual(Manufacturer, model.Manufacturer.Name);
        }


        [TestMethod]
        public async Task GetMissingTestAsync()
        {
            var model = await _manager.GetAsync(a => a.IATA == "Missing");
            Assert.IsNull(model);
        }

        [TestMethod]
        public async Task ListAllTestAsync()
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
        public async Task ListMissingTestAsync()
        {
            var models = await _manager.ListAsync(x => x.IATA == "Missing");
            Assert.IsEmpty(models);
        }
    }
}


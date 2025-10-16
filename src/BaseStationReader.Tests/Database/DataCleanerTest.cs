using BaseStationReader.BusinessLogic.Database;
using BaseStationReader.Data;
using BaseStationReader.Interfaces.Database;
using BaseStationReader.Tests.Mocks;

namespace BaseStationReader.Tests.Database
{
    [TestClass]
    public class DataCleanerTest
    {
        private const string ModelName = "   airbus a300-600ST    \"super transporter\" / \"beluga\"   ";
    
        private IDatabaseManagementFactory _factory;

        [TestInitialize]
        public async Task Initialise()
        {
            // Create the factory for database acces
            var context = BaseStationReaderDbContextFactory.CreateInMemoryDbContext();
            var logger = new MockFileLogger();
            _factory = new DatabaseManagementFactory(logger, context, 0, 0);

            // Add the objects to be cleaned
            await context.Airlines.AddAsync(new() { IATA = "ba", ICAO = "baw", Name = "brITish \nairWAYS  " });
            await context.Manufacturers.AddAsync(new() { Id = 1, Name = "  haWKer SiDDeley \r\naviation ltd.   " });
            await context.SaveChangesAsync();
            await context.Models.AddAsync(new() { IATA = "abb", ICAO = "a3st", Name = ModelName, ManufacturerId = 1 });
            await context.SaveChangesAsync();
        }

        [TestMethod]
        public async Task CleanAirlinesTestAsync()
        {
            await _factory.DataCleaner.CleanAirlines();
            var airlines = await _factory.AirlineManager.ListAsync(x => true);
            Assert.AreEqual("BA", airlines[0].IATA);
            Assert.AreEqual("BAW", airlines[0].ICAO);
            Assert.AreEqual("British Airways", airlines[0].Name);
        }

        [TestMethod]
        public async Task CleanManufacturersTestAsync()
        {
            await _factory.DataCleaner.CleanManufacturers();
            var manufacturers = await _factory.ManufacturerManager.ListAsync(x => true);
            Assert.AreEqual("Hawker Siddeley Aviation Ltd.", manufacturers[0].Name);
        }

        [TestMethod]
        public async Task CleanModelsTestAsync()
        {
            await _factory.DataCleaner.CleanModels();
            var models = await _factory.ModelManager.ListAsync(x => true);
            Assert.AreEqual("ABB", models[0].IATA);
            Assert.AreEqual("A3ST", models[0].ICAO);
            Assert.AreEqual(ModelName, models[0].Name);
        }
    }
}
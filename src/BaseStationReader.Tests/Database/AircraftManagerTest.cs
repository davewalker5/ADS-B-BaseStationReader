using BaseStationReader.Data;
using BaseStationReader.BusinessLogic.Database;
using BaseStationReader.Entities.Api;
using BaseStationReader.Interfaces.Database;

namespace BaseStationReader.Tests.Database
{
    [TestClass]
    public class AircraftManagerTest
    {
        private readonly string Address = new Random().Next(0, 16777215).ToString("X6");
        private const string Manufacturer = "Airbus";
        private const string ModelIATA = "332";
        private const string ModelICAO = "A332";
        private const string ModelName = "A330-200";
        private const string Registration = "G-ABCD";
        private const int Manufactured = 2014;

        private IAircraftManager _manager = null;
        private Model _model;
        private readonly int _age = DateTime.Now.Year - Manufactured;

        [TestInitialize]
        public async Task InitialiseAsync()
        {
            // Create a context and an aircraft management class to test
            BaseStationReaderDbContext context = BaseStationReaderDbContextFactory.CreateInMemoryDbContext();
            _manager = new AircraftManager(context);

            // Set up a manufacturer, an aircraft model and an aircraft
            var manufacturer = await new ManufacturerManager(context).AddAsync(Manufacturer);
            _model = await new ModelManager(context).AddAsync(ModelIATA, ModelICAO, ModelName, manufacturer.Id);
            _ = await _manager.AddAsync(Address, Registration, Manufactured, _age, _model.Id);
        }

        [TestMethod]
        public async Task AddDuplicateTestAsync()
        {
            await _manager.AddAsync(Address, Registration, Manufactured, _age, _model.Id);
            var aircraft = await _manager.ListAsync(x => true);
            Assert.HasCount(1, aircraft);
        }

        [TestMethod]
        public async Task AddAndGetTestAsync()
        {
            var aircraft = await _manager.GetAsync(a => a.Address == Address);
            Assert.IsNotNull(aircraft);
            Assert.IsGreaterThan(0, aircraft.Id);
            Assert.AreEqual(Address, aircraft.Address);
            Assert.AreEqual(Registration, aircraft.Registration);
            Assert.AreEqual(Manufactured, aircraft.Manufactured);
            Assert.AreEqual(_age, aircraft.Age);
            Assert.AreEqual(ModelIATA, aircraft.Model.IATA);
            Assert.AreEqual(ModelICAO, aircraft.Model.ICAO);
            Assert.AreEqual(ModelName, aircraft.Model.Name);
            Assert.AreEqual(Manufacturer, aircraft.Model.Manufacturer.Name);
            Assert.AreEqual("MANUAL", aircraft.Provenance.SourceRef);
        }

        [TestMethod]
        public async Task GetMissingTestAsync()
        {
            var aircraft = await _manager.GetAsync(a => a.Address == "Missing");
            Assert.IsNull(aircraft);
        }

        [TestMethod]
        public async Task ListAllTestAsync()
        {
            var aircraft = await _manager.ListAsync(x => true);
            Assert.IsNotNull(aircraft);
            Assert.HasCount(1, aircraft);
            Assert.IsGreaterThan(0, aircraft[0].Id);
            Assert.AreEqual(Address, aircraft[0].Address);
            Assert.AreEqual(Registration, aircraft[0].Registration);
            Assert.AreEqual(Manufactured, aircraft[0].Manufactured);
            Assert.AreEqual(_age, aircraft[0].Age);
            Assert.AreEqual(ModelIATA, aircraft[0].Model.IATA);
            Assert.AreEqual(ModelICAO, aircraft[0].Model.ICAO);
            Assert.AreEqual(ModelName, aircraft[0].Model.Name);
            Assert.AreEqual(Manufacturer, aircraft[0].Model.Manufacturer.Name);
        }

        [TestMethod]
        public async Task ListMissingTestAsync()
        {
            var aircraft = await _manager.ListAsync(x => x.Address == "Missing");
            Assert.IsEmpty(aircraft);
        }

        [TestMethod]
        public async Task AddWithMissingProvenanceTestAsync()
        {
            await Assert.ThrowsExactlyAsync<InvalidOperationException>(() =>
                _manager.AddAsync("ABC123", Registration, Manufactured, _age, _model.Id, 999));
        }

        [TestMethod]
        public async Task UpdateTestAsync()
        {
            var aircraft = await _manager.GetAsync(a => a.Address == Address);
            var updated = await _manager.UpdateAsync(
                aircraft.Id, "ABC123", "G-WXYZ", 2020, 6, _model.Id, aircraft.ProvenanceId);

            Assert.AreEqual("ABC123", updated.Address);
            Assert.AreEqual("G-WXYZ", updated.Registration);
            Assert.AreEqual(2020, updated.Manufactured);
            Assert.AreEqual(6, updated.Age);
        }

        [TestMethod]
        public async Task DeleteTestAsync()
        {
            var aircraft = await _manager.GetAsync(a => a.Address == Address);
            await _manager.DeleteAsync(aircraft.Id);
            Assert.IsEmpty(await _manager.ListAsync(x => true));
        }
    }
}

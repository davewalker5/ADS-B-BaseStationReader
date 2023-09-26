using BaseStationReader.Data;
using BaseStationReader.Entities.Interfaces;
using BaseStationReader.Entities.Lookup;
using BaseStationReader.Logic.Database;

namespace BaseStationReader.Tests
{
    [TestClass]
    public class AircraftModelReaderTest
    {
        private IAircraftModelReader? _reader = null;

        [TestInitialize]
        public void Initialise()
        {
            BaseStationReaderDbContext context = BaseStationReaderDbContextFactory.CreateInMemoryDbContext();
            _reader = new AircraftModelReader(context);

            // Set up a Wake Turbulence Category
            context.WakeTurbulenceCategories.Add(new WakeTurbulenceCategory
            {
                Category = "H",
                Meaning = "High"
            });
            context.SaveChanges();
            var wtcId = context.WakeTurbulenceCategories.First().Id;

            // Set up a manufacturer
            context.Manufacturers.Add(new Manufacturer
            {
                Name = "Airbus"
            });
            context.SaveChanges();
            var manufacturerId = context.Manufacturers.First().Id;

            // Add two aircraft models
            context.AircraftModels.Add(new AircraftModel
            {
                IATA = "332",
                ICAO = "A332",
                Name = "A330-200",
                ManufacturerId = manufacturerId,
                WakeTurbulenceCategoryId = wtcId
            });

            context.AircraftModels.Add(new AircraftModel
            {
                IATA = "345",
                ICAO = "A345",
                Name = "A340-500",
                ManufacturerId = manufacturerId,
                WakeTurbulenceCategoryId = wtcId
            });
            context.SaveChanges();
        }

        [TestMethod]
        public void GetAircraftByIATATest()
        {
            var aircraft = Task.Run(() => _reader!.GetAsync(x => x.IATA == "332")).Result;
            Assert.AreEqual("332", aircraft.IATA);
            Assert.AreEqual("A332", aircraft.ICAO);
            Assert.AreEqual("A330-200", aircraft.Name);
            Assert.AreEqual("Airbus", aircraft.Manufacturer.Name);
            Assert.AreEqual("H", aircraft.WakeTurbulenceCategory!.Category);
        }

        [TestMethod]
        public void GetAircraftByICAOTest()
        {
            var aircraft = Task.Run(() => _reader!.GetAsync(x => x.ICAO == "A345")).Result;
            Assert.AreEqual("345", aircraft.IATA);
            Assert.AreEqual("A345", aircraft.ICAO);
            Assert.AreEqual("A340-500", aircraft.Name);
            Assert.AreEqual("Airbus", aircraft.Manufacturer.Name);
            Assert.AreEqual("H", aircraft.WakeTurbulenceCategory!.Category);
        }

        [TestMethod]
        public void GetAircraftByNameTest()
        {
            var aircraft = Task.Run(() => _reader!.GetAsync(x => x.Name == "A330-200")).Result;
            Assert.AreEqual("332", aircraft.IATA);
            Assert.AreEqual("A332", aircraft.ICAO);
            Assert.AreEqual("A330-200", aircraft.Name);
            Assert.AreEqual("Airbus", aircraft.Manufacturer.Name);
            Assert.AreEqual("H", aircraft.WakeTurbulenceCategory!.Category);
        }

        [TestMethod]
        public void ListAircraftByManufacturerTest()
        {
            var aircraft = Task.Run(() => _reader!.ListAsync(x => x.Manufacturer.Name == "Airbus")).Result;
            Assert.AreEqual(2, aircraft.Count);
            Assert.IsNotNull(aircraft.Find(x => x.IATA == "332"));
            Assert.IsNotNull(aircraft.Find(x => x.IATA == "345"));
        }
    }
}

using BaseStationReader.Entities.Tracking;
using BaseStationReader.BusinessLogic.DataExchange;

namespace BaseStationReader.Tests
{
    [TestClass]
    public class ExporterTest
    {
        private IList<Aircraft> _aircraft = new List<Aircraft>();
        private IList<AircraftPosition> _positions = new List<AircraftPosition>();

        [TestMethod]
        public void ExportAircraftCsvTest()
        {
            GenerateRandomAircraft();

            var filepath = Path.ChangeExtension(Path.GetTempFileName(), "csv");
            new CsvExporter<Aircraft>().Export(_aircraft, filepath, ',');

            var info = new FileInfo(filepath);
            Assert.AreEqual(info.FullName, filepath);
            Assert.IsTrue(info.Length > 0);

            File.Delete(filepath);
        }

        [TestMethod]
        public void ExportAircraftXlsxTest()
        {
            GenerateRandomAircraft();

            var filepath = Path.ChangeExtension(Path.GetTempFileName(), "xlsx");
            new XlsxExporter<Aircraft>().Export(_aircraft, filepath, "Aircraft");

            var info = new FileInfo(filepath);
            Assert.AreEqual(info.FullName, filepath);
            Assert.IsTrue(info.Length > 0);

            File.Delete(filepath);
        }

        [TestMethod]
        public void ExportPositionsCsvTest()
        {
            GenerateRandomPositions();

            var filepath = Path.ChangeExtension(Path.GetTempFileName(), "csv");
            new CsvExporter<AircraftPosition>().Export(_positions, filepath, ',');

            var info = new FileInfo(filepath);
            Assert.AreEqual(info.FullName, filepath);
            Assert.IsTrue(info.Length > 0);

            File.Delete(filepath);
        }

        [TestMethod]
        public void ExportPositionXlsxTest()
        {
            GenerateRandomPositions();

            var filepath = Path.ChangeExtension(Path.GetTempFileName(), "xlsx");
            new XlsxExporter<AircraftPosition>().Export(_positions, filepath, "Positions");

            var info = new FileInfo(filepath);
            Assert.AreEqual(info.FullName, filepath);
            Assert.IsTrue(info.Length > 0);

            File.Delete(filepath);
        }

        private void GenerateRandomAircraft()
        {
            var random = new Random();

            _aircraft.Clear();
            for (int i = 0; i < 10; i++)
            {
                var address = random.Next(0, 16777215).ToString("X6");

                _aircraft.Add(new Aircraft
                {
                    Address = address,
                    FirstSeen = DateTime.Now,
                    LastSeen = DateTime.Now
                });
            }
        }

        private void GenerateRandomPositions()
        {
            var random = new Random();
            var address = random.Next(0, 16777215).ToString("X6");

            _positions.Clear();
            for (int i = 0; i < 10; i++)
            {
                _positions.Add(new AircraftPosition
                {
                    Address = address,
                    Altitude = (decimal)random.Next(0, 41000),
                    Latitude = (decimal)random.Next(-90, 90),
                    Longitude = (decimal)random.Next(-180, 180),
                    Timestamp = DateTime.Now
                });
            }
        }
    }
}

using BaseStationReader.Entities.Tracking;
using BaseStationReader.Logic.DataExchange;

namespace BaseStationReader.Tests
{
    [TestClass]
    public class AircraftExporterTest
    {
        private IList<Aircraft> _aircraft = new List<Aircraft>();

        [TestInitialize]
        public void TestInitialise()
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

        [TestMethod]
        public void ExportAircraftCsvTest()
        {
            var filepath = Path.ChangeExtension(Path.GetTempFileName(), "csv");
            new AircraftCsvExporter().Export(_aircraft, filepath, ',');

            var info = new FileInfo(filepath);
            Assert.AreEqual(info.FullName, filepath);
            Assert.IsTrue(info.Length > 0);

            File.Delete(filepath);
        }

        [TestMethod]
        public void ExportAircraftXlsxTest()
        {
            var filepath = Path.ChangeExtension(Path.GetTempFileName(), "xlsx");
            new AircraftXlsxExporter().Export(_aircraft, filepath);

            var info = new FileInfo(filepath);
            Assert.AreEqual(info.FullName, filepath);
            Assert.IsTrue(info.Length > 0);

            File.Delete(filepath);
        }
    }
}

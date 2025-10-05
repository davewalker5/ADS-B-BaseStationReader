using System.Globalization;
using BaseStationReader.BusinessLogic.Export;
using BaseStationReader.Entities.Api;
using CsvHelper;

namespace BaseStationReader.Tests.DataExchange
{
    [TestClass]
    public class FlightNumberExporterTest
    {
        private const string Callsign = "KLM123XY";
        private const string FlightNumber = "KLM123";
        private readonly DateTime _date = DateTime.Now;

        private string _filePath;

        [TestCleanup]
        public void CleanUp()
        {
            if (!string.IsNullOrEmpty(_filePath) && File.Exists(_filePath))
            {
                File.Delete(_filePath);
            }
        }

        [TestMethod]
        public void ExportTest()
        {
            List<FlightNumber> flightNumbers = [new()
            {
                Callsign = Callsign,
                Number = FlightNumber,
                Date = _date
            }];

            _filePath = Path.ChangeExtension(Path.GetTempFileName(), "csv");
            new FlightNumberExporter().Export(flightNumbers, _filePath);

            List<FlightNumber> records;
            using (var reader = new StreamReader(_filePath))
            {
                using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                {
                    records = [.. csv.GetRecords<FlightNumber>()];
                }
            }

            Assert.IsNotNull(records);
            Assert.HasCount(1, records);
            Assert.AreEqual(Callsign, records[0].Callsign);
            Assert.AreEqual(FlightNumber, records[0].Number);
            Assert.AreEqual(_date.ToShortDateString(), records[0].Date.Value.ToShortDateString());
        }
    }
}
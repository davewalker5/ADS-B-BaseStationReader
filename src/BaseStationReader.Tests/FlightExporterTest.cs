using System.Globalization;
using BaseStationReader.BusinessLogic.Export;
using BaseStationReader.Entities.Lookup;
using CsvHelper;

namespace BaseStationReader.Tests
{
    [TestClass]
    public class FlightExporterTest
    {
        private const string AircraftAddress = "4851F6";
        private const string FlightICAO = "KLM743";
        private const string FlightIATA = "KL743";
        private const string FlightNumber = "743";
        private const string Embarkation = "AMS";
        private const string Destination = "LIM";
        private const string AirlineName = "KLM";

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
            List<Flight> flights = [new()
            {
                AircraftAddress = AircraftAddress,
                Number = FlightNumber,
                ICAO = FlightICAO,
                IATA = FlightIATA,
                Embarkation = Embarkation,
                Destination = Destination,
                Airline = new()
                {
                    Name = AirlineName
                }
            }];

            _filePath = Path.ChangeExtension(Path.GetTempFileName(), "csv");
            new FlightExporter().Export(flights, _filePath);

            List<dynamic> records;
            using (var reader = new StreamReader(_filePath))
            {
                using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                {
                    records = [.. csv.GetRecords<dynamic>()];
                }
            }

            Assert.IsNotNull(records);
            Assert.HasCount(1, records);
            Assert.AreEqual(AircraftAddress, records[0].AircraftAddress);
            Assert.AreEqual(FlightNumber, records[0].Number);
            Assert.AreEqual(FlightICAO, records[0].ICAO);
            Assert.AreEqual(FlightIATA, records[0].IATA);
            Assert.AreEqual(Embarkation, records[0].Embarkation);
            Assert.AreEqual(Destination, records[0].Destination);
            Assert.AreEqual(AirlineName, records[0].AirlineName);
        }
    }
}
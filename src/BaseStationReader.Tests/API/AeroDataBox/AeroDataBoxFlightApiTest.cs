using BaseStationReader.Api.AirLabs;
using BaseStationReader.Entities.Api;
using BaseStationReader.Tests.Mocks;
using BaseStationReader.Interfaces.Api;
using BaseStationReader.Entities.Config;
using System.Globalization;
using BaseStationReader.BusinessLogic.Database;
using BaseStationReader.Data;

namespace BaseStationReader.Tests.API.AeroDataBox
{
    [TestClass]
    public class AeroDataBoxFlightApiTest
    {
        private const string Address = "4074B6";
        private const string Registration = "G-UZHF";
        private const string LastSeen = "2025-09-25";
        private const string Response = "[ { \"greatCircleDistance\": { \"meter\": 1679537.5, \"km\": 1679.54, \"mile\": 1043.62, \"nm\": 906.88, \"feet\": 5510293.62 }, \"departure\": { \"airport\": { \"icao\": \"EGCC\", \"iata\": \"MAN\", \"name\": \"Manchester\", \"shortName\": \"Manchester\", \"municipalityName\": \"Manchester\", \"location\": { \"lat\": 53.3537, \"lon\": -2.27495 }, \"countryCode\": \"GB\", \"timeZone\": \"Europe/London\" }, \"scheduledTime\": { \"utc\": \"2025-09-25 07:20Z\", \"local\": \"2025-09-25 08:20+01:00\" }, \"revisedTime\": { \"utc\": \"2025-09-25 07:15Z\", \"local\": \"2025-09-25 08:15+01:00\" }, \"runwayTime\": { \"utc\": \"2025-09-25 07:45Z\", \"local\": \"2025-09-25 08:45+01:00\" }, \"terminal\": \"1\", \"gate\": \"4\", \"runway\": \"05L\", \"quality\": [ \"Basic\", \"Live\" ] }, \"arrival\": { \"airport\": { \"icao\": \"LIRF\", \"iata\": \"FCO\", \"name\": \"Rome Leonardo da Vinci–Fiumicino\", \"shortName\": \"Leonardo da Vinci–Fiumicino\", \"municipalityName\": \"Rome\", \"location\": { \"lat\": 41.8045, \"lon\": 12.2508 }, \"countryCode\": \"IT\", \"timeZone\": \"Europe/Rome\" }, \"scheduledTime\": { \"utc\": \"2025-09-25 10:10Z\", \"local\": \"2025-09-25 12:10+02:00\" }, \"revisedTime\": { \"utc\": \"2025-09-25 10:04Z\", \"local\": \"2025-09-25 12:04+02:00\" }, \"predictedTime\": { \"utc\": \"2025-09-25 09:56Z\", \"local\": \"2025-09-25 11:56+02:00\" }, \"terminal\": \"1\", \"runway\": \"16R\", \"quality\": [ \"Basic\", \"Live\" ] }, \"lastUpdatedUtc\": \"2025-09-25 10:11Z\", \"number\": \"U2 2123\", \"callSign\": \"EZY12ND\", \"status\": \"Approaching\", \"codeshareStatus\": \"IsOperator\", \"isCargo\": false, \"aircraft\": { \"reg\": \"G-UZHF\", \"modeS\": \"4074B6\", \"model\": \"Airbus A320 (Sharklets)\" }, \"airline\": { \"name\": \"easyJet\", \"iata\": \"U2\", \"icao\": \"EZY\" } }, { \"greatCircleDistance\": { \"meter\": 1679537.5, \"km\": 1679.54, \"mile\": 1043.62, \"nm\": 906.88, \"feet\": 5510293.62 }, \"departure\": { \"airport\": { \"icao\": \"LIRF\", \"iata\": \"FCO\", \"name\": \"Rome Leonardo da Vinci–Fiumicino\", \"shortName\": \"Leonardo da Vinci–Fiumicino\", \"municipalityName\": \"Rome\", \"location\": { \"lat\": 41.8045, \"lon\": 12.2508 }, \"countryCode\": \"IT\", \"timeZone\": \"Europe/Rome\" }, \"scheduledTime\": { \"utc\": \"2025-09-25 11:00Z\", \"local\": \"2025-09-25 13:00+02:00\" }, \"revisedTime\": { \"utc\": \"2025-09-25 12:16Z\", \"local\": \"2025-09-25 14:16+02:00\" }, \"runwayTime\": { \"utc\": \"2025-09-25 12:16Z\", \"local\": \"2025-09-25 14:16+02:00\" }, \"terminal\": \"1\", \"runway\": \"25\", \"quality\": [ \"Basic\", \"Live\" ] }, \"arrival\": { \"airport\": { \"icao\": \"EGCC\", \"iata\": \"MAN\", \"name\": \"Manchester\", \"shortName\": \"Manchester\", \"municipalityName\": \"Manchester\", \"location\": { \"lat\": 53.3537, \"lon\": -2.27495 }, \"countryCode\": \"GB\", \"timeZone\": \"Europe/London\" }, \"scheduledTime\": { \"utc\": \"2025-09-25 13:50Z\", \"local\": \"2025-09-25 14:50+01:00\" }, \"revisedTime\": { \"utc\": \"2025-09-25 14:42Z\", \"local\": \"2025-09-25 15:42+01:00\" }, \"terminal\": \"1\", \"gate\": \"9\", \"quality\": [ \"Basic\", \"Live\" ] }, \"lastUpdatedUtc\": \"2025-09-25 14:47Z\", \"number\": \"U2 2124\", \"callSign\": \"EZY38DT\", \"status\": \"Arrived\", \"codeshareStatus\": \"IsOperator\", \"isCargo\": false, \"aircraft\": { \"reg\": \"G-UZHF\", \"modeS\": \"4074B6\", \"model\": \"Airbus A320 (Sharklets)\" }, \"airline\": { \"name\": \"easyJet\", \"iata\": \"U2\", \"icao\": \"EZY\" } } ]";

        private readonly DateTime _lastSeenUtc = DateTime.SpecifyKind(
            DateTime.ParseExact(LastSeen, "yyyy-MM-dd", CultureInfo.InvariantCulture),
            DateTimeKind.Utc
        );

        private MockTrackerHttpClient _client = null;
        private IFlightApi _api = null;

        private readonly ExternalApiSettings _settings = new()
        {
            ApiServices = [
                new ApiService()
                {
                    Service = ApiServiceType.AeroDataBox, Key = "an-api-key",
                    ApiEndpoints = [
                        new ApiEndpoint() { EndpointType = ApiEndpointType.Flights, Url = "http://some.host.com/endpoint"}
                    ]
                }
            ]
        };

        [TestInitialize]
        public void Initialise()
        {
            var context = BaseStationReaderDbContextFactory.CreateInMemoryDbContext();
            var logger = new MockFileLogger();
            var factory = new DatabaseManagementFactory(logger, context, 0, 0);
            _client = new MockTrackerHttpClient();
            _api = new AeroDataBoxFlightApi(_client, factory, _settings);
        }
        
        [TestMethod]
        public async Task GetHistoricalFlightsTestAsync()
        {
            _client.AddResponse(Response);
            var properties = await _api.LookupFlightsAsync(Address, _lastSeenUtc);

            Assert.IsNotNull(properties);
            Assert.HasCount(2, properties);

            Assert.HasCount(12, properties[0]);
            Assert.AreEqual("U22123", properties[0][ApiProperty.FlightIATA]);
            Assert.IsEmpty(properties[0][ApiProperty.FlightICAO]);
            Assert.AreEqual("MAN", properties[0][ApiProperty.EmbarkationIATA]);
            Assert.AreEqual("2025-09-25 07:45Z", properties[0][ApiProperty.DepartureTime]);
            Assert.AreEqual("FCO", properties[0][ApiProperty.DestinationIATA]);
            Assert.AreEqual("2025-09-25 10:04Z", properties[0][ApiProperty.ArrivalTime]);
            Assert.AreEqual("easyJet", properties[0][ApiProperty.AirlineName]);
            Assert.AreEqual("U2", properties[0][ApiProperty.AirlineIATA]);
            Assert.AreEqual("EZY", properties[0][ApiProperty.AirlineICAO]);
            Assert.AreEqual(Address, properties[0][ApiProperty.AircraftAddress]);
            Assert.AreEqual(Registration, properties[0][ApiProperty.AircraftRegistration]);
            Assert.IsEmpty(properties[0][ApiProperty.ModelICAO]);

            Assert.HasCount(12, properties[1]);
            Assert.AreEqual("U22124", properties[1][ApiProperty.FlightIATA]);
            Assert.IsEmpty(properties[1][ApiProperty.FlightICAO]);
            Assert.AreEqual("FCO", properties[1][ApiProperty.EmbarkationIATA]);
            Assert.AreEqual("2025-09-25 12:16Z", properties[1][ApiProperty.DepartureTime]);
            Assert.AreEqual("MAN", properties[1][ApiProperty.DestinationIATA]);
            Assert.AreEqual("2025-09-25 14:42Z", properties[1][ApiProperty.ArrivalTime]);
            Assert.AreEqual("easyJet", properties[1][ApiProperty.AirlineName]);
            Assert.AreEqual("U2", properties[1][ApiProperty.AirlineIATA]);
            Assert.AreEqual("EZY", properties[1][ApiProperty.AirlineICAO]);
            Assert.AreEqual(Address, properties[0][ApiProperty.AircraftAddress]);
            Assert.AreEqual(Registration, properties[0][ApiProperty.AircraftRegistration]);
            Assert.IsEmpty(properties[1][ApiProperty.ModelICAO]);
        }

        [TestMethod]
        public async Task InvalidJsonResponseTestAsync()
        {
            _client.AddResponse("{}");
            var properties = await _api.LookupFlightsAsync(Address, _lastSeenUtc);

            Assert.IsNull(properties);
        }

        [TestMethod]
        public async Task NullResponseTestAsync()
        {
            _client.AddResponse(null);
            var properties = await _api.LookupFlightsAsync(Address, _lastSeenUtc);

            Assert.IsNull(properties);
        }
    }
}
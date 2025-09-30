using BaseStationReader.BusinessLogic.Api.AirLabs;
using BaseStationReader.Entities.Api;
using BaseStationReader.Tests.Mocks;
using BaseStationReader.Interfaces.Api;
using BaseStationReader.Entities.Config;

namespace BaseStationReader.Tests.API.AeroDataBox
{
    [TestClass]
    public class AeroDataBoxHistoricalFlightApiTest
    {
        private const string Address = "408181";
        private const string Response = "[ { \"greatCircleDistance\": { \"meter\": 1679537.5, \"km\": 1679.54, \"mile\": 1043.62, \"nm\": 906.88, \"feet\": 5510293.62 }, \"departure\": { \"airport\": { \"icao\": \"EGCC\", \"iata\": \"MAN\", \"name\": \"Manchester\", \"shortName\": \"Manchester\", \"municipalityName\": \"Manchester\", \"location\": { \"lat\": 53.3537, \"lon\": -2.27495 }, \"countryCode\": \"GB\", \"timeZone\": \"Europe/London\" }, \"scheduledTime\": { \"utc\": \"2025-09-25 07:20Z\", \"local\": \"2025-09-25 08:20+01:00\" }, \"revisedTime\": { \"utc\": \"2025-09-25 07:15Z\", \"local\": \"2025-09-25 08:15+01:00\" }, \"runwayTime\": { \"utc\": \"2025-09-25 07:45Z\", \"local\": \"2025-09-25 08:45+01:00\" }, \"terminal\": \"1\", \"gate\": \"4\", \"runway\": \"05L\", \"quality\": [ \"Basic\", \"Live\" ] }, \"arrival\": { \"airport\": { \"icao\": \"LIRF\", \"iata\": \"FCO\", \"name\": \"Rome Leonardo da Vinci–Fiumicino\", \"shortName\": \"Leonardo da Vinci–Fiumicino\", \"municipalityName\": \"Rome\", \"location\": { \"lat\": 41.8045, \"lon\": 12.2508 }, \"countryCode\": \"IT\", \"timeZone\": \"Europe/Rome\" }, \"scheduledTime\": { \"utc\": \"2025-09-25 10:10Z\", \"local\": \"2025-09-25 12:10+02:00\" }, \"revisedTime\": { \"utc\": \"2025-09-25 10:04Z\", \"local\": \"2025-09-25 12:04+02:00\" }, \"predictedTime\": { \"utc\": \"2025-09-25 09:56Z\", \"local\": \"2025-09-25 11:56+02:00\" }, \"terminal\": \"1\", \"runway\": \"16R\", \"quality\": [ \"Basic\", \"Live\" ] }, \"lastUpdatedUtc\": \"2025-09-25 10:11Z\", \"number\": \"U2 2123\", \"callSign\": \"EZY12ND\", \"status\": \"Approaching\", \"codeshareStatus\": \"IsOperator\", \"isCargo\": false, \"aircraft\": { \"reg\": \"G-UZHF\", \"modeS\": \"4074B6\", \"model\": \"Airbus A320 (Sharklets)\" }, \"airline\": { \"name\": \"easyJet\", \"iata\": \"U2\", \"icao\": \"EZY\" } }, { \"greatCircleDistance\": { \"meter\": 1679537.5, \"km\": 1679.54, \"mile\": 1043.62, \"nm\": 906.88, \"feet\": 5510293.62 }, \"departure\": { \"airport\": { \"icao\": \"LIRF\", \"iata\": \"FCO\", \"name\": \"Rome Leonardo da Vinci–Fiumicino\", \"shortName\": \"Leonardo da Vinci–Fiumicino\", \"municipalityName\": \"Rome\", \"location\": { \"lat\": 41.8045, \"lon\": 12.2508 }, \"countryCode\": \"IT\", \"timeZone\": \"Europe/Rome\" }, \"scheduledTime\": { \"utc\": \"2025-09-25 11:00Z\", \"local\": \"2025-09-25 13:00+02:00\" }, \"revisedTime\": { \"utc\": \"2025-09-25 12:16Z\", \"local\": \"2025-09-25 14:16+02:00\" }, \"runwayTime\": { \"utc\": \"2025-09-25 12:16Z\", \"local\": \"2025-09-25 14:16+02:00\" }, \"terminal\": \"1\", \"runway\": \"25\", \"quality\": [ \"Basic\", \"Live\" ] }, \"arrival\": { \"airport\": { \"icao\": \"EGCC\", \"iata\": \"MAN\", \"name\": \"Manchester\", \"shortName\": \"Manchester\", \"municipalityName\": \"Manchester\", \"location\": { \"lat\": 53.3537, \"lon\": -2.27495 }, \"countryCode\": \"GB\", \"timeZone\": \"Europe/London\" }, \"scheduledTime\": { \"utc\": \"2025-09-25 13:50Z\", \"local\": \"2025-09-25 14:50+01:00\" }, \"revisedTime\": { \"utc\": \"2025-09-25 14:42Z\", \"local\": \"2025-09-25 15:42+01:00\" }, \"terminal\": \"1\", \"gate\": \"9\", \"quality\": [ \"Basic\", \"Live\" ] }, \"lastUpdatedUtc\": \"2025-09-25 14:47Z\", \"number\": \"U2 2124\", \"callSign\": \"EZY38DT\", \"status\": \"Arrived\", \"codeshareStatus\": \"IsOperator\", \"isCargo\": false, \"aircraft\": { \"reg\": \"G-UZHF\", \"modeS\": \"4074B6\", \"model\": \"Airbus A320 (Sharklets)\" }, \"airline\": { \"name\": \"easyJet\", \"iata\": \"U2\", \"icao\": \"EZY\" } } ]";

        private MockTrackerHttpClient _client = null;
        private IHistoricalFlightsApi _api = null;

        private readonly ExternalApiSettings _settings = new()
        {
            ApiServices = [
                new ApiService() { Service = ApiServiceType.AeroDataBox, Key = "Some API Key"},
                new ApiService() { Service = ApiServiceType.AirLabs, Key = "Some API Key"}
            ],
            ApiEndpoints = [
                new ApiEndpoint() { Service = ApiServiceType.AeroDataBox, EndpointType = ApiEndpointType.Aircraft, Url = "http://some.host.com/endpoint"},
                new ApiEndpoint() { Service = ApiServiceType.AeroDataBox, EndpointType = ApiEndpointType.HistoricalFlights, Url = "http://some.host.com/endpoint"},
                new ApiEndpoint() { Service = ApiServiceType.AirLabs, EndpointType = ApiEndpointType.Aircraft, Url = "http://some.host.com/endpoint"},
                new ApiEndpoint() { Service = ApiServiceType.AirLabs, EndpointType = ApiEndpointType.Airlines, Url = "http://some.host.com/endpoint"},
                new ApiEndpoint() { Service = ApiServiceType.AirLabs, EndpointType = ApiEndpointType.ActiveFlights, Url = "http://some.host.com/endpoint"}
            ]
        };

        [TestInitialize]
        public void Initialise()
        {
            var logger = new MockFileLogger();
            _client = new MockTrackerHttpClient();
            _api = new AeroDataBoxHistoricalFlightApi(logger, _client, _settings);
        }
        
        [TestMethod]
        public void GetHistoricalFlightsTest()
        {
            _client.AddResponse(Response);
            var properties = Task.Run(() => _api.LookupFlightsByAircraftAsync(Address)).Result;

            Assert.IsNotNull(properties);
            Assert.HasCount(2, properties);

            Assert.HasCount(12, properties[0]);
            Assert.IsEmpty(properties[0][ApiProperty.FlightIATA]);
            Assert.IsEmpty(properties[0][ApiProperty.FlightICAO]);
            Assert.AreEqual("U22123", properties[0][ApiProperty.FlightNumber]);
            Assert.AreEqual("MAN", properties[0][ApiProperty.EmbarkationIATA]);
            Assert.AreEqual("2025-09-25 07:45Z", properties[0][ApiProperty.DepartureTime]);
            Assert.AreEqual("FCO", properties[0][ApiProperty.DestinationIATA]);
            Assert.AreEqual("2025-09-25 10:04Z", properties[0][ApiProperty.ArrivalTime]);
            Assert.AreEqual("easyJet", properties[0][ApiProperty.AirlineName]);
            Assert.AreEqual("U2", properties[0][ApiProperty.AirlineIATA]);
            Assert.AreEqual("EZY", properties[0][ApiProperty.AirlineICAO]);
            Assert.AreEqual("4074B6", properties[0][ApiProperty.AircraftAddress]);
            Assert.IsEmpty(properties[0][ApiProperty.ModelICAO]);

            Assert.HasCount(12, properties[1]);
            Assert.IsEmpty(properties[1][ApiProperty.FlightIATA]);
            Assert.IsEmpty(properties[1][ApiProperty.FlightICAO]);
            Assert.AreEqual("U22124", properties[1][ApiProperty.FlightNumber]);
            Assert.AreEqual("FCO", properties[1][ApiProperty.EmbarkationIATA]);
            Assert.AreEqual("2025-09-25 12:16Z", properties[1][ApiProperty.DepartureTime]);
            Assert.AreEqual("MAN", properties[1][ApiProperty.DestinationIATA]);
            Assert.AreEqual("2025-09-25 14:42Z", properties[1][ApiProperty.ArrivalTime]);
            Assert.AreEqual("easyJet", properties[1][ApiProperty.AirlineName]);
            Assert.AreEqual("U2", properties[1][ApiProperty.AirlineIATA]);
            Assert.AreEqual("EZY", properties[1][ApiProperty.AirlineICAO]);
            Assert.AreEqual("4074B6", properties[0][ApiProperty.AircraftAddress]);
            Assert.IsEmpty(properties[1][ApiProperty.ModelICAO]);
        }

        [TestMethod]
        public void InvalidJsonResponseTest()
        {
            _client.AddResponse("{}");
            var properties = Task.Run(() => _api.LookupFlightsByAircraftAsync(Address)).Result;

            Assert.IsNull(properties);
        }

        [TestMethod]
        public void ClientExceptionTest()
        {
            _client.AddResponse(null);
            var properties = Task.Run(() => _api.LookupFlightsByAircraftAsync(Address)).Result;

            Assert.IsNull(properties);
        }
    }
}
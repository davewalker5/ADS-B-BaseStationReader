using BaseStationReader.Entities.Api;
using BaseStationReader.BusinessLogic.Api.AirLabs;
using BaseStationReader.Tests.Mocks;
using BaseStationReader.Interfaces.Api;
using BaseStationReader.Entities.Config;

namespace BaseStationReader.Tests.API.AirLabs
{
    [TestClass]
    public class AirLabsActiveFlightApiTest
    {
        private const string Address = "4005C1";
        private const string Response = "{ \"response\": [ { \"hex\": \"4005C1\", \"flag\": \"UK\", \"lat\": 54.001557, \"lng\": -15.078022, \"alt\": 12516, \"dir\": 93, \"speed\": 900, \"flight_number\": \"172\", \"flight_icao\": \"BAW172\", \"flight_iata\": \"BA172\", \"dep_icao\": \"KJFK\", \"dep_iata\": \"JFK\", \"arr_icao\": \"EGLL\", \"arr_iata\": \"LHR\", \"airline_icao\": \"BAW\", \"airline_iata\": \"BA\", \"aircraft_icao\": \"B772\", \"updated\": 1758434637, \"status\": \"en-route\", \"type\": \"adsb\" } ]}";

        private MockTrackerHttpClient _client = null;
        private IActiveFlightsApi _api = null;

        private readonly ExternalApiSettings _settings = new()
        {
            ApiServices = [
                new ApiService() { Service = ApiServiceType.AirLabs, Key = "an-api-key"}
            ],
            ApiEndpoints = [
                new ApiEndpoint() { Service = ApiServiceType.AirLabs, EndpointType = ApiEndpointType.ActiveFlights, Url = "http://some.host.com/endpoint"}
            ]
        };

        [TestInitialize]
        public void Initialise()
        {
            var logger = new MockFileLogger();
            _client = new MockTrackerHttpClient();
            _api = new AirLabsActiveFlightApi(logger, _client, null, _settings);
        }

        [TestMethod]
        public void GetActiveFlightTest()
        {
            _client.AddResponse(Response);
            var properties = Task.Run(() => _api.LookupFlightAsync(ApiProperty.AircraftAddress, Address)).Result;

            Assert.IsNotNull(properties);
            Assert.HasCount(10, properties);
            Assert.AreEqual("JFK", properties[ApiProperty.EmbarkationIATA]);
            Assert.AreEqual("LHR", properties[ApiProperty.DestinationIATA]);
            Assert.AreEqual("BA172", properties[ApiProperty.FlightIATA]);
            Assert.AreEqual("BAW172", properties[ApiProperty.FlightICAO]);
            Assert.AreEqual("BA172", properties[ApiProperty.FlightNumber]);
            Assert.AreEqual("BA", properties[ApiProperty.AirlineIATA]);
            Assert.AreEqual("BAW", properties[ApiProperty.AirlineICAO]);
            Assert.IsEmpty(properties[ApiProperty.AirlineName]);
            Assert.AreEqual("B772", properties[ApiProperty.ModelICAO]);
            Assert.AreEqual("4005C1", properties[ApiProperty.AircraftAddress]);
        }

        [TestMethod]
        public void InvalidJsonResponseTest()
        {
            _client.AddResponse("{}");
            var properties = Task.Run(() => _api.LookupFlightAsync(ApiProperty.AircraftAddress, Address)).Result;

            Assert.IsNull(properties);
        }

        [TestMethod]
        public void ClientExceptionTest()
        {
            _client.AddResponse(null);
            var properties = Task.Run(() => _api.LookupFlightAsync(ApiProperty.AircraftAddress, Address)).Result;

            Assert.IsNull(properties);
        }
    }
}

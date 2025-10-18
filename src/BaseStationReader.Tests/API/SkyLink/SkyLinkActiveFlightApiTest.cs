using BaseStationReader.Entities.Api;
using BaseStationReader.Tests.Mocks;
using BaseStationReader.Interfaces.Api;
using BaseStationReader.Entities.Config;
using BaseStationReader.BusinessLogic.Api.SkyLink;
using BaseStationReader.BusinessLogic.Database;

namespace BaseStationReader.Tests.API.SkyLink
{
    [TestClass]
    public class SkyLinkActiveFlightApiTest
    {
        private const string FlightIATA = "KL701";
        private const string Response = "{ \"flight_number\": \"KL701\", \"status\": \"Departed 21:12\", \"airline\": \"KLM\", \"departure\": { \"airport\": \"AMS • Amsterdam\", \"airport_full\": \"Amsterdam Schiphol Airport\", \"scheduled_time\": \"20:50\", \"scheduled_date\": \"02 Oct\", \"actual_time\": \"21:12\", \"actual_date\": \"02 Oct\", \"terminal\": \"2\", \"gate\": \"F4\", \"checkin\": \"--\" }, \"arrival\": { \"airport\": \"EZE • Buenos Aires\", \"airport_full\": \"Buenos Aires Ministro Pistarini Airport\", \"scheduled_time\": \"05:30\", \"scheduled_date\": \"03 Oct\", \"estimated_time\": \"05:19\", \"estimated_date\": \"03 Oct\", \"terminal\": \"IA\", \"gate\": \"--\", \"baggage\": \"--\" } }";

        private MockTrackerHttpClient _client = null;
        private IActiveFlightsApi _api = null;

        private readonly ExternalApiSettings _settings = new()
        {
            ApiServices = [
                new ApiService() { Service = ApiServiceType.SkyLink, Key = "an-api-key"}
            ],
            ApiEndpoints = [
                new ApiEndpoint() { Service = ApiServiceType.SkyLink, EndpointType = ApiEndpointType.ActiveFlights, Url = "http://some.host.com/endpoint"}
            ]
        };

        [TestInitialize]
        public void Initialise()
        {
            var logger = new MockFileLogger();
            var factory = new DatabaseManagementFactory(logger, null, 0, 0);
            _client = new MockTrackerHttpClient();
            _api = new SkyLinkActiveFlightApi(_client, factory, _settings);
        }

        [TestMethod]
        public async Task GetActiveFlightTestAsync()
        {
            _client.AddResponse(Response);
            var properties = await _api.LookupFlightAsync(ApiProperty.FlightIATA, FlightIATA);

            Assert.IsNotNull(properties);
            Assert.HasCount(6, properties);
            Assert.IsEmpty(properties[ApiProperty.FlightICAO]);
            Assert.AreEqual(FlightIATA, properties[ApiProperty.FlightIATA]);
            Assert.AreEqual("KL", properties[ApiProperty.AirlineIATA]);
            Assert.IsEmpty(properties[ApiProperty.AirlineICAO]);
            Assert.AreEqual("AMS", properties[ApiProperty.EmbarkationIATA]);
            Assert.AreEqual("EZE", properties[ApiProperty.DestinationIATA]);
        }

        [TestMethod]
        public async Task NullResponseTestAsync()
        {
            _client.AddResponse(null);
            var properties = await _api.LookupFlightAsync(ApiProperty.FlightIATA, FlightIATA);

            Assert.IsNull(properties);
        }

        [TestMethod]
        public async Task InvalidJsonResponseTestAsync()
        {
            _client.AddResponse("{}");
            var properties = await _api.LookupFlightAsync(ApiProperty.FlightIATA, FlightIATA);

            Assert.IsNull(properties);
        }
    }
}

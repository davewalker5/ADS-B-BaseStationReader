using BaseStationReader.Entities.Api;
using BaseStationReader.Tests.Mocks;
using BaseStationReader.Interfaces.Api;
using BaseStationReader.Entities.Config;
using BaseStationReader.BusinessLogic.Api.SkyLink;

namespace BaseStationReader.Tests.API.SkyLink
{
    [TestClass]
    public class SkyLinkActiveFlightApiTest
    {
        private const string Number = "KL701";
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
            _client = new MockTrackerHttpClient();
            _api = new SkyLinkActiveFlightApi(logger, _client, null, _settings);
        }

        [TestMethod]
        public void GetActiveFlightTest()
        {
            _client.AddResponse(Response);
            var properties = Task.Run(() => _api.LookupFlight(ApiProperty.FlightNumber, Number)).Result;

            Assert.IsNotNull(properties);
            Assert.HasCount(4, properties);
            Assert.AreEqual(Number, properties[ApiProperty.FlightNumber]);
            Assert.AreEqual("KLM", properties[ApiProperty.AirlineICAO]);
            Assert.AreEqual("AMS", properties[ApiProperty.EmbarkationIATA]);
            Assert.AreEqual("EZE", properties[ApiProperty.DestinationIATA]);
        }

        [TestMethod]
        public void InvalidJsonResponseTest()
        {
            _client.AddResponse("{}");
            var properties = Task.Run(() => _api.LookupFlight(ApiProperty.FlightNumber, Number)).Result;

            Assert.IsNull(properties);
        }

        [TestMethod]
        public void ClientExceptionTest()
        {
            _client.AddResponse(null);
            var properties = Task.Run(() => _api.LookupFlight(ApiProperty.FlightNumber, Number)).Result;

            Assert.IsNull(properties);
        }
    }
}

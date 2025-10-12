using BaseStationReader.Entities.Api;
using BaseStationReader.Tests.Mocks;
using BaseStationReader.Interfaces.Api;
using BaseStationReader.Entities.Config;
using BaseStationReader.BusinessLogic.Api.SkyLink;
using System.Threading.Tasks;

namespace BaseStationReader.Tests.API.SkyLink
{
    [TestClass]
    public class SkyLinkAirlinesApiTest
    {
        private const string Response = "[ { \"id\": 1355, \"name\": \"British Airways\", \"alias\": null, \"iata\": \"BA\", \"icao\": \"BAW\", \"callsign\": \"SPEEDBIRD\", \"country\": \"United Kingdom\", \"active\": \"Y\", \"logo\": \"https://media.skylinkapi.com/logos/BA.png\" } ]";
        private const string NoIATACode = "[ { \"id\": 1355, \"name\": \"British Airways\", \"alias\": null, \"iata\": \"\", \"icao\": \"BAW\", \"callsign\": \"SPEEDBIRD\", \"country\": \"United Kingdom\", \"active\": \"Y\", \"logo\": \"https://media.skylinkapi.com/logos/BA.png\" } ]";
        private const string NoICAOCode = "[ { \"id\": 1355, \"name\": \"British Airways\", \"alias\": null, \"iata\": \"BA\", \"icao\": \"\", \"callsign\": \"SPEEDBIRD\", \"country\": \"United Kingdom\", \"active\": \"Y\", \"logo\": \"https://media.skylinkapi.com/logos/BA.png\" } ]";

        private MockTrackerHttpClient _client = null;
        private IAirlinesApi _api = null;

        private readonly ExternalApiSettings _settings = new()
        {
            ApiServices = [
                new ApiService() { Service = ApiServiceType.SkyLink, Key = "an-api-key"}
            ],
            ApiEndpoints = [
                new ApiEndpoint() { Service = ApiServiceType.SkyLink, EndpointType = ApiEndpointType.Airlines, Url = "http://some.host.com/endpoint"}
            ]
        };

        [TestInitialize]
        public void Initialise()
        {
            var logger = new MockFileLogger();
            _client = new MockTrackerHttpClient();
            _api = new SkyLinkAirlinesApi(logger, _client, null, _settings);
        }

        [TestMethod]
        public async Task GetAirlineByIATACodeTestAsync()
        {
            _client.AddResponse(Response);
            var properties = await _api.LookupAirlineByIATACodeAsync("BA");

            Assert.IsNotNull(properties);
            Assert.HasCount(3, properties);
            Assert.AreEqual("BA", properties[ApiProperty.AirlineIATA]);
            Assert.AreEqual("BAW", properties[ApiProperty.AirlineICAO]);
            Assert.AreEqual("British Airways", properties[ApiProperty.AirlineName]);
        }

        [TestMethod]
        public async Task GetAirlineByICAOCodeTestAsync()
        {
            _client.AddResponse(Response);
            var properties = await _api.LookupAirlineByICAOCodeAsync("BAW");

            Assert.IsNotNull(properties);
            Assert.HasCount(3, properties);
            Assert.AreEqual("BA", properties[ApiProperty.AirlineIATA]);
            Assert.AreEqual("BAW", properties[ApiProperty.AirlineICAO]);
            Assert.AreEqual("British Airways", properties[ApiProperty.AirlineName]);
        }

        [TestMethod]
        public async Task NoIATACodeTestAsync()
        {
            _client.AddResponse(NoIATACode);
            var properties = await _api.LookupAirlineByICAOCodeAsync("BAW");

            Assert.IsNotNull(properties);
            Assert.HasCount(3, properties);
            Assert.AreEqual("", properties[ApiProperty.AirlineIATA]);
            Assert.AreEqual("BAW", properties[ApiProperty.AirlineICAO]);
            Assert.AreEqual("British Airways", properties[ApiProperty.AirlineName]);
        }

        [TestMethod]
        public async Task NoICAOCodeTestAsync()
        {
            _client.AddResponse(NoICAOCode);
            var properties = await _api.LookupAirlineByICAOCodeAsync("BAW");

            Assert.IsNotNull(properties);
            Assert.HasCount(3, properties);
            Assert.AreEqual("BA", properties[ApiProperty.AirlineIATA]);
            Assert.AreEqual("", properties[ApiProperty.AirlineICAO]);
            Assert.AreEqual("British Airways", properties[ApiProperty.AirlineName]);
        }

        [TestMethod]
        public async Task NullResponseTestAsync()
        {
            _client.AddResponse(null);
            var properties = await _api.LookupAirlineByICAOCodeAsync("BAW");

            Assert.IsNull(properties);
        }

        [TestMethod]
        public async Task InvalidJsonResponseTestAsync()
        {
            _client.AddResponse("[]");
            var properties = await _api.LookupAirlineByICAOCodeAsync("BAW");

            Assert.IsNull(properties);
        }
    }
}

using BaseStationReader.Entities.Api;
using BaseStationReader.Tests.Mocks;
using BaseStationReader.Interfaces.Api;
using BaseStationReader.Entities.Config;
using BaseStationReader.BusinessLogic.Api.SkyLink;

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
        public void GetAirlineByIATACodeTest()
        {
            _client.AddResponse(Response);
            var properties = Task.Run(() => _api.LookupAirlineByIATACodeAsync("BA")).Result;

            Assert.IsNotNull(properties);
            Assert.HasCount(3, properties);
            Assert.AreEqual("BA", properties[ApiProperty.AirlineIATA]);
            Assert.AreEqual("BAW", properties[ApiProperty.AirlineICAO]);
            Assert.AreEqual("British Airways", properties[ApiProperty.AirlineName]);
        }

        [TestMethod]
        public void GetAirlineByICAOCodeTest()
        {
            _client.AddResponse(Response);
            var properties = Task.Run(() => _api.LookupAirlineByICAOCodeAsync("BAW")).Result;

            Assert.IsNotNull(properties);
            Assert.HasCount(3, properties);
            Assert.AreEqual("BA", properties[ApiProperty.AirlineIATA]);
            Assert.AreEqual("BAW", properties[ApiProperty.AirlineICAO]);
            Assert.AreEqual("British Airways", properties[ApiProperty.AirlineName]);
        }

        [TestMethod]
        public void NoIATACodeTest()
        {
            _client.AddResponse(NoIATACode);
            var properties = Task.Run(() => _api.LookupAirlineByICAOCodeAsync("BAW")).Result;

            Assert.IsNotNull(properties);
            Assert.HasCount(3, properties);
            Assert.AreEqual("", properties[ApiProperty.AirlineIATA]);
            Assert.AreEqual("BAW", properties[ApiProperty.AirlineICAO]);
            Assert.AreEqual("British Airways", properties[ApiProperty.AirlineName]);
        }

        [TestMethod]
        public void NoICAOCodeTest()
        {
            _client.AddResponse(NoICAOCode);
            var properties = Task.Run(() => _api.LookupAirlineByICAOCodeAsync("BAW")).Result;

            Assert.IsNotNull(properties);
            Assert.HasCount(3, properties);
            Assert.AreEqual("BA", properties[ApiProperty.AirlineIATA]);
            Assert.AreEqual("", properties[ApiProperty.AirlineICAO]);
            Assert.AreEqual("British Airways", properties[ApiProperty.AirlineName]);
        }

        [TestMethod]
        public void InvalidJsonResponseTest()
        {
            _client.AddResponse("[]");
            var properties = Task.Run(() => _api.LookupAirlineByICAOCodeAsync("BAW")).Result;

            Assert.IsNull(properties);
        }

        [TestMethod]
        public void ClientExceptionTest()
        {
            _client.AddResponse(null);
            var properties = Task.Run(() => _api.LookupAirlineByICAOCodeAsync("BAW")).Result;

            Assert.IsNull(properties);
        }
    }
}

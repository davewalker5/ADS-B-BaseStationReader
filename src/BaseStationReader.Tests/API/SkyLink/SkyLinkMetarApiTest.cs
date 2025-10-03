using BaseStationReader.Tests.Mocks;
using BaseStationReader.Interfaces.Api;
using BaseStationReader.Entities.Config;
using BaseStationReader.BusinessLogic.Api.SkyLink;

namespace BaseStationReader.Tests.API.SkyLink
{
    [TestClass]
    public class SkyLinkMetarApiTest
    {
        private const string AirportICAO = "EGLL";
        private const string METAR = "METAR EGLL 031150Z COR AUTO 19011KT 150V240 9999 BKN005 OVC009 17/16 Q1009 NOSIG";
        private const string Response = "{ \"raw\": \"METAR EGLL 031150Z COR AUTO 19011KT 150V240 9999 BKN005 OVC009 17/16 Q1009 NOSIG\", \"icao\": \"EGLL\", \"airport_name\": \"London Heathrow Airport\", \"timestamp\": \"2025-10-03T12:23:39.122722Z\" }";

        private MockTrackerHttpClient _client = null;
        private IMetarApi _api = null;

        private readonly ExternalApiSettings _settings = new()
        {
            ApiServices = [
                new ApiService() { Service = ApiServiceType.SkyLink, Key = "an-api-key"}
            ],
            ApiEndpoints = [
                new ApiEndpoint() { Service = ApiServiceType.SkyLink, EndpointType = ApiEndpointType.METAR, Url = "http://some.host.com/endpoint"}
            ]
        };

        [TestInitialize]
        public void Initialise()
        {
            var logger = new MockFileLogger();
            _client = new MockTrackerHttpClient();
            _api = new SkyLinkMetarApi(logger, _client, _settings);
        }

        [TestMethod]
        public void GetWeatherTest()
        {
            _client.AddResponse(Response);
            var results = Task.Run(() => _api.LookupAirportWeather(AirportICAO)).Result;

            Assert.IsNotNull(results);
            Assert.HasCount(1, results);
            Assert.AreEqual(METAR, results.First());
        }

        [TestMethod]
        public void InvalidJsonResponseTest()
        {
            _client.AddResponse("{}");
            var results = Task.Run(() => _api.LookupAirportWeather(AirportICAO)).Result;

            Assert.IsNull(results);
        }

        [TestMethod]
        public void ClientExceptionTest()
        {
            _client.AddResponse(null);
            var properties = Task.Run(() => _api.LookupAirportWeather(AirportICAO)).Result;

            Assert.IsNull(properties);
        }
    }
}

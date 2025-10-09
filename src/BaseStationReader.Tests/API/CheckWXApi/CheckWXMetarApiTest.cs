using BaseStationReader.Tests.Mocks;
using BaseStationReader.Interfaces.Api;
using BaseStationReader.Entities.Config;
using BaseStationReader.BusinessLogic.Api.CheckWXApi;

namespace BaseStationReader.Tests.API.AirLabs
{
    [TestClass]
    public class CheckWXMetarApiTest
    {
        private const string AirportICAO = "EGLL";
        private const string METAR = "METAR EGLL 291150Z COR AUTO VRB03KT 9999 SCT028 16/09 Q1026 NOSIG";
        private const string Response = "{ \"results\": 1, \"data\": [ \"METAR EGLL 291150Z COR AUTO VRB03KT 9999 SCT028 16/09 Q1026 NOSIG\" ] }";

        private MockTrackerHttpClient _client = null;
        private IMetarApi _api = null;

        private readonly ExternalApiSettings _settings = new()
        {
            ApiServices = [
                new ApiService() { Service = ApiServiceType.CheckWXApi, Key = "an-api-key"}
            ],
            ApiEndpoints = [
                new ApiEndpoint() { Service = ApiServiceType.CheckWXApi, EndpointType = ApiEndpointType.METAR, Url = "http://some.host.com/endpoint"}
            ]
        };

        [TestInitialize]
        public void Initialise()
        {
            var logger = new MockFileLogger();
            _client = new MockTrackerHttpClient();
            _api = new CheckWXMetarApi(logger, _client, null, _settings);
        }

        [TestMethod]
        public void GetWeatherTest()
        {
            _client.AddResponse(Response);
            var results = Task.Run(() => _api.LookupCurrentAirportWeatherAsync(AirportICAO)).Result;

            Assert.IsNotNull(results);
            Assert.HasCount(1, results);
            Assert.AreEqual(METAR, results.First());
        }

        [TestMethod]
        public void InvalidJsonResponseTest()
        {
            _client.AddResponse("{}");
            var results = Task.Run(() => _api.LookupCurrentAirportWeatherAsync(AirportICAO)).Result;

            Assert.IsNull(results);
        }

        [TestMethod]
        public void ClientExceptionTest()
        {
            _client.AddResponse(null);
            var properties = Task.Run(() => _api.LookupCurrentAirportWeatherAsync(AirportICAO)).Result;

            Assert.IsNull(properties);
        }
    }
}

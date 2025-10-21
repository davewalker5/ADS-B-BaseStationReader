using BaseStationReader.Api.Wrapper;
using BaseStationReader.BusinessLogic.Database;
using BaseStationReader.Data;
using BaseStationReader.Entities.Config;
using BaseStationReader.Interfaces.Api;
using BaseStationReader.Interfaces.Database;
using BaseStationReader.Tests.Mocks;

namespace BaseStationReader.Tests.API.Wrapper
{
    [TestClass]
    public class ExternalApiFactoryTest
    {
        private readonly MockFileLogger _logger = new();
        private readonly MockTrackerHttpClient _client = new();
        private IDatabaseManagementFactory _factory;
        private IExternalApiFactory _apiFactory;

        private readonly ExternalApiSettings _settings = new()
        {
            ApiServices = [
                new ApiService() { Service = ApiServiceType.AeroDataBox, Key = "an-api-key"},
                new ApiService() { Service = ApiServiceType.AirLabs, Key = "an-api-key"}
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
            var context = BaseStationReaderDbContextFactory.CreateInMemoryDbContext();
            _factory = new DatabaseManagementFactory(_logger, context, 0, 0);
            _apiFactory = new ExternalApiFactory();
        }

        [TestMethod]
        public void GetAeroDataBoxHistoricalFlightsApiInstanceTest()
        {
            var api = _apiFactory.GetApiInstance(
                ApiServiceType.AeroDataBox, ApiEndpointType.HistoricalFlights, _logger, _client, null, _settings)
                as IHistoricalFlightsApi;
            Assert.IsTrue(api is IHistoricalFlightsApi);
        }

        [TestMethod]
        public void GetAeroDataBoxAircraftApiInstanceTest()
        {
            var api = _apiFactory.GetApiInstance(
                ApiServiceType.AeroDataBox, ApiEndpointType.Aircraft, _logger, _client, null, _settings)
                as IAircraftApi;
            Assert.IsTrue(api is IAircraftApi);
        }

        [TestMethod]
        public void GetAirLabsActiveFlightsApiInstanceTest()
        {
            var api = _apiFactory.GetApiInstance(
                ApiServiceType.AirLabs, ApiEndpointType.ActiveFlights, _logger, _client, null, _settings)
                as IActiveFlightsApi;
            Assert.IsTrue(api is IActiveFlightsApi);
        }

        [TestMethod]
        public void GetAirLabsAirlinesApiInstanceTest()
        {
            var api = _apiFactory.GetApiInstance(
                ApiServiceType.AirLabs, ApiEndpointType.Airlines, _logger, _client, null, _settings)
                as IAirlinesApi;
            Assert.IsTrue(api is IAirlinesApi);
        }

        [TestMethod]
        public void GetAirLabsAircraftApiInstanceTest()
        {
            var api = _apiFactory.GetApiInstance(
                ApiServiceType.AirLabs, ApiEndpointType.Aircraft, _logger, _client, null, _settings)
                as IAircraftApi;
            Assert.IsTrue(api is IAircraftApi);
        }

        [TestMethod]
        public void GetAeroDataBoxApiWrapperInstanceTest()
        {
            var wrapper = _apiFactory.GetWrapperInstance(
                _logger, _client, _factory, ApiServiceType.AeroDataBox, ApiEndpointType.HistoricalFlights, _settings, false);
            Assert.IsNotNull(wrapper);
        }

        [TestMethod]
        public void GetAirLabsApiWrapperInstanceTest()
        {
            var wrapper = _apiFactory.GetWrapperInstance(
                _logger, _client, _factory, ApiServiceType.AirLabs, ApiEndpointType.ActiveFlights, _settings, false);
            Assert.IsNotNull(wrapper);
        }

        [TestMethod]
        public void GetCheckWXApiWrapperInstanceTest()
        {
            var wrapper = _apiFactory.GetWrapperInstance(
                _logger, _client, _factory, ApiServiceType.CheckWXApi, ApiEndpointType.ActiveFlights, _settings, false);
            Assert.IsNotNull(wrapper);
        }
    }
}
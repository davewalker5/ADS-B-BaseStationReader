using System.Diagnostics.CodeAnalysis;
using BaseStationReader.BusinessLogic.Api;
using BaseStationReader.Data;
using BaseStationReader.Entities.Config;
using BaseStationReader.Interfaces.Api;
using BaseStationReader.Tests.Mocks;

namespace BaseStationReader.Tests.API
{
    [ExcludeFromCodeCoverage]
    [TestClass]
    public class ExternalApiFactoryTest
    {
        private readonly MockFileLogger _logger = new();
        private readonly BaseStationReaderDbContext _context = BaseStationReaderDbContextFactory.CreateInMemoryDbContext();
        private readonly MockTrackerHttpClient _client = new();

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

        [TestMethod]
        public void GetAeroDataBoxHistoricalFlightsApiInstanceTest()
        {
            var api = ExternalApiFactory.GetApiInstance(
                ApiServiceType.AeroDataBox, ApiEndpointType.HistoricalFlights, _logger, _client, _settings)
                as IHistoricalFlightsApi;
            Assert.IsTrue(api is IHistoricalFlightsApi);
        }

        [TestMethod]
        public void GetAeroDataBoxAircraftApiInstanceTest()
        {
            var api = ExternalApiFactory.GetApiInstance(
                ApiServiceType.AeroDataBox, ApiEndpointType.Aircraft, _logger, _client, _settings)
                as IAircraftApi;
            Assert.IsTrue(api is IAircraftApi);
        }

        [TestMethod]
        public void GetAirLabsActiveFlightsApiInstanceTest()
        {
            var api = ExternalApiFactory.GetApiInstance(
                ApiServiceType.AirLabs, ApiEndpointType.ActiveFlights, _logger, _client, _settings)
                as IActiveFlightsApi;
            Assert.IsTrue(api is IActiveFlightsApi);
        }

        [TestMethod]
        public void GetAirLabsAirlinesApiInstanceTest()
        {
            var api = ExternalApiFactory.GetApiInstance(
                ApiServiceType.AirLabs, ApiEndpointType.Airlines, _logger, _client, _settings)
                as IAirlinesApi;
            Assert.IsTrue(api is IAirlinesApi);
        }

        [TestMethod]
        public void GetAirLabsAircraftApiInstanceTest()
        {
            var api = ExternalApiFactory.GetApiInstance(
                ApiServiceType.AirLabs, ApiEndpointType.Aircraft, _logger, _client, _settings)
                as IAircraftApi;
            Assert.IsTrue(api is IAircraftApi);
        }

        [TestMethod]
        public void GetAeroDataBoxApiWrapperInstanceTest()
        {
            var wrapper = ExternalApiFactory.GetWrapperInstance(
                _logger, _client, _context, null, ApiServiceType.AeroDataBox, ApiEndpointType.HistoricalFlights, _settings);
            Assert.IsNotNull(wrapper);
        }

        [TestMethod]
        public void GetAirLabsApiWrapperInstanceTest()
        {
            var wrapper = ExternalApiFactory.GetWrapperInstance(
                _logger, _client, _context, null, ApiServiceType.AirLabs, ApiEndpointType.ActiveFlights, _settings);
            Assert.IsNotNull(wrapper);
        }
    }
}
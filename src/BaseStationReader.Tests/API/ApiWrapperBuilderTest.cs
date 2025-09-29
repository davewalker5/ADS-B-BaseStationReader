using System.Diagnostics.CodeAnalysis;
using BaseStationReader.BusinessLogic.Api;
using BaseStationReader.BusinessLogic.Api.AeroDatabox;
using BaseStationReader.BusinessLogic.Api.AirLabs;
using BaseStationReader.Data;
using BaseStationReader.Entities.Config;
using BaseStationReader.Tests.Mocks;

namespace BaseStationReader.Tests.API
{
    [ExcludeFromCodeCoverage]
    [TestClass]
    public class ApiWrapperBuilderTest
    {
        private readonly MockFileLogger _logger = new();
        private readonly BaseStationReaderDbContext _context = BaseStationReaderDbContextFactory.CreateInMemoryDbContext();
        private readonly MockTrackerHttpClient _client = new();

        private readonly ExternalApiSettings _settings = new()
        {
            ApiServiceKeys = [
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
        
        [TestMethod]
        public void BuildNoneWrapperFromTypeTest()
        {
            var wrapper = ApiWrapperBuilder.GetInstance(_logger, _settings, _context, _client, ApiServiceType.None);
            Assert.IsNull(wrapper);
        }

        [TestMethod]
        public void BuildNoneWrapperFromStringTest()
        {
            var wrapper = ApiWrapperBuilder.GetInstance(_logger, _settings, _context, _client, "None");
            Assert.IsNull(wrapper);
        }

        [TestMethod]
        public void BuildNoneWrapperFromRandomStringTest()
        {
            var wrapper = ApiWrapperBuilder.GetInstance(_logger, _settings, _context, _client, "Not a service type name");
            Assert.IsNull(wrapper);
        }

        [TestMethod]
        public void GetNoneServiceTypeFromStringTest()
        {
            var type = ApiWrapperBuilder.GetServiceTypeFromString("None");
            Assert.AreEqual(ApiServiceType.None, type);
        }

        [TestMethod]
        public void GetNoneServiceTypeFromRandomStringTest()
        {
            var type = ApiWrapperBuilder.GetServiceTypeFromString("Not a service type name");
            Assert.AreEqual(ApiServiceType.None, type);
        }

        [TestMethod]
        public void BuildAirLabsApiWrapperFromTypeTest()
        {
            var wrapper = ApiWrapperBuilder.GetInstance(_logger, _settings, _context, _client, ApiServiceType.AirLabs);
            Assert.IsNotNull(wrapper);
            Assert.IsTrue(wrapper is AirLabsApiWrapper);
        }

        [TestMethod]
        public void BuildAirLabsApiWrapperFromStringTest()
        {
            var wrapper = ApiWrapperBuilder.GetInstance(_logger, _settings, _context, _client, "AirLabs");
            Assert.IsNotNull(wrapper);
            Assert.IsTrue(wrapper is AirLabsApiWrapper);
        }

        [TestMethod]
        public void GetAirLabsServiceTypeFromStringTest()
        {
            var type = ApiWrapperBuilder.GetServiceTypeFromString("AirLabs");
            Assert.AreEqual(ApiServiceType.AirLabs, type);
        }

        [TestMethod]
        public void BuildAeroDataBoxApiWrapperFromTypeTest()
        {
            var wrapper = ApiWrapperBuilder.GetInstance(_logger, _settings, _context, _client, ApiServiceType.AeroDataBox);
            Assert.IsNotNull(wrapper);
            Assert.IsTrue(wrapper is AeroDataBoxApiWrapper);
        }

        [TestMethod]
        public void BuildAeroDataBoxApiWrapperFromStringTest()
        {
            var wrapper = ApiWrapperBuilder.GetInstance(_logger, _settings, _context, _client, "AeroDataBox");
            Assert.IsNotNull(wrapper);
            Assert.IsTrue(wrapper is AeroDataBoxApiWrapper);
        }

        [TestMethod]
        public void GetAeroDataBoxServiceTypeFromStringTest()
        {
            var type = ApiWrapperBuilder.GetServiceTypeFromString("AeroDataBox");
            Assert.AreEqual(ApiServiceType.AeroDataBox, type);
        }
    }
}
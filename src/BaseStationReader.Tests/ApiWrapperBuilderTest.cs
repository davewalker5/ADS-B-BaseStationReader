using System.Diagnostics.CodeAnalysis;
using BaseStationReader.BusinessLogic.Api;
using BaseStationReader.BusinessLogic.Api.AirLabs;
using BaseStationReader.Entities.Config;

namespace BaseStationReader.Tests
{
    [ExcludeFromCodeCoverage]
    [TestClass]
    public class ApiWrapperBuilderTest
    {
        [TestMethod]
        public void BuildNoneWrapperFromTypeTest()
        {
            var wrapper = ApiWrapperBuilder.GetInstance(ApiServiceType.None);
            Assert.IsNull(wrapper);
        }

        [TestMethod]
        public void BuildNoneWrapperFromStringTest()
        {
            var wrapper = ApiWrapperBuilder.GetInstance("None");
            Assert.IsNull(wrapper);
        }

        [TestMethod]
        public void BuildNoneWrapperFromRandomStringTest()
        {
            var wrapper = ApiWrapperBuilder.GetInstance("Not a service type name");
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
            var wrapper = ApiWrapperBuilder.GetInstance(ApiServiceType.AirLabs);
            Assert.IsNotNull(wrapper);
            Assert.IsTrue(wrapper is AirLabsApiWrapper);
        }

        [TestMethod]
        public void BuildAirLabsApiWrapperFromStringTest()
        {
            var wrapper = ApiWrapperBuilder.GetInstance("AirLabs");
            Assert.IsNotNull(wrapper);
            Assert.IsTrue(wrapper is AirLabsApiWrapper);
        }

        [TestMethod]
        public void GetAirLabsServiceTypeFromStringTest()
        {
            var type = ApiWrapperBuilder.GetServiceTypeFromString("AirLabs");
            Assert.AreEqual(ApiServiceType.AirLabs, type);
        }
    }
}
using BaseStationReader.BusinessLogic.Database;
using BaseStationReader.Data;
using BaseStationReader.Entities.Api;
using BaseStationReader.Entities.Config;
using BaseStationReader.Interfaces.Database;

namespace BaseStationReader.Tests.Database
{
    [TestClass]
    public class ApiLogManagerTest
    {
        private const string Address = "394A08";
        private const string Url = "https://some.host.com/";

        private IApiLogManager _manager = null;

        [TestInitialize]
        public void Initialise()
        {
            BaseStationReaderDbContext context = BaseStationReaderDbContextFactory.CreateInMemoryDbContext();
            _manager = new ApiLogManager(context);
        }

        [TestMethod]
        public async Task LoggingTestAsync()
        {
            await _manager.AddAsync(
                ApiServiceType.SkyLink,
                ApiEndpointType.Aircraft,
                Url,
                ApiProperty.AircraftAddress,
                Address);

            var entries = await _manager.ListAsync(x => true);

            Assert.IsNotNull(entries);
            Assert.HasCount(1, entries);
            Assert.AreEqual("SkyLink", entries[0].Service);
            Assert.AreEqual("Aircraft", entries[0].Endpoint);
            Assert.AreEqual(Url, entries[0].Url);
            Assert.AreEqual("AircraftAddress", entries[0].Property);
            Assert.AreEqual(Address, entries[0].PropertyValue);
        }
    }
}
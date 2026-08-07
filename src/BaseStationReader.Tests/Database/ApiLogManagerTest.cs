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

        [TestMethod]
        public async Task SearchFiltersAndPagesNewestFirstTestAsync()
        {
            await AddEntryAsync(ApiServiceType.SkyLink, ApiEndpointType.Aircraft, "FIRST");
            await AddEntryAsync(ApiServiceType.AeroDataBox, ApiEndpointType.Flights, "SECOND");
            await AddEntryAsync(ApiServiceType.AeroDataBox, ApiEndpointType.Flights, "THIRD");

            var result = await _manager.SearchAsync(new ApiLogFilter
            {
                Service = nameof(ApiServiceType.AeroDataBox),
                Endpoint = nameof(ApiEndpointType.Flights),
                FromDate = DateTime.Today,
                ToDate = DateTime.Today,
                Page = 1,
                PageSize = 1
            });

            Assert.AreEqual(2, result.TotalCount);
            Assert.AreEqual(2, result.TotalPages);
            Assert.HasCount(1, result.Items);
            Assert.AreEqual("THIRD", result.Items[0].PropertyValue);
        }

        [TestMethod]
        public async Task SearchRejectsReversedDateRangeTestAsync()
        {
            var filter = new ApiLogFilter
            {
                FromDate = DateTime.Today,
                ToDate = DateTime.Today.AddDays(-1)
            };

            await Assert.ThrowsExactlyAsync<ArgumentException>(() => _manager.SearchAsync(filter));
        }

        [TestMethod]
        public async Task ClearDeletesAllEntriesTestAsync()
        {
            await AddEntryAsync(ApiServiceType.SkyLink, ApiEndpointType.METAR, "EGLL");
            await AddEntryAsync(ApiServiceType.CheckWXApi, ApiEndpointType.METAR, "EGCC");

            var deleted = await _manager.ClearAsync();
            var entries = await _manager.ListAsync(entry => true);

            Assert.AreEqual(2, deleted);
            Assert.IsEmpty(entries);
        }

        /// <summary>
        /// Adds a log entry with the supplied request dimensions.
        /// </summary>
        /// <param name="service">The external service.</param>
        /// <param name="endpoint">The endpoint type.</param>
        /// <param name="value">The logged property value.</param>
        private async Task AddEntryAsync(
            ApiServiceType service,
            ApiEndpointType endpoint,
            string value)
        {
            await _manager.AddAsync(service, endpoint, Url, ApiProperty.AircraftAddress, value);
        }
    }
}

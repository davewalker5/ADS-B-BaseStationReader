using BaseStationReader.Api.Wrapper;
using BaseStationReader.BusinessLogic.Database;
using BaseStationReader.Data;
using BaseStationReader.Entities.Api;
using BaseStationReader.Entities.Config;
using BaseStationReader.Entities.Tracking;
using BaseStationReader.Interfaces.Api;
using BaseStationReader.Interfaces.Database;
using BaseStationReader.Interfaces.Logging;
using BaseStationReader.Tests.Mocks;
using Moq;

namespace BaseStationReader.Tests.API.Wrapper
{
    [TestClass]
    public class HistoricalLookupEligibilityAssessorTest
    {
        private ITrackerLogger _logger;
        private IDatabaseManagementFactory _factory;

        private const string ValidAddress = "4005CO";
        private const string Callsign = "BAW49N";
        private const string InvalidAddress = "Not a valid 24-bit ICAO address";

        [TestInitialize]
        public void Initialise()
        {
            _logger = new MockFileLogger();
            var context = BaseStationReaderDbContextFactory.CreateInMemoryDbContext();
            _factory = new DatabaseManagementFactory(_logger, context, 0, 0);
        }

        [TestMethod]
        public async Task HistoricalLookupByInvalidAddressTestAsync()
        {
            var historicalFlightApiWrapper = new Mock<IHistoricalFlightApiWrapper>();
            historicalFlightApiWrapper.Setup(x => x.SupportsLookupBy(ApiProperty.AircraftAddress)).Returns(true);
            var activeFlightApiWrapper = new Mock<IActiveFlightApiWrapper>();
            var assessor = new LookupEligibilityAssessor(
                historicalFlightApiWrapper.Object,
                activeFlightApiWrapper.Object,
                _factory,
                false);

            var result = await assessor.IsEligibleForLookupAsync(ApiEndpointType.HistoricalFlights, InvalidAddress);
            Assert.IsFalse(result.Eligible);
            Assert.IsFalse(result.Requeue);
        }

        [TestMethod]
        public async Task HistoricalLookupByAddressTestAsync()
        {
            var historicalFlightApiWrapper = new Mock<IHistoricalFlightApiWrapper>();
            historicalFlightApiWrapper.Setup(x => x.SupportsLookupBy(ApiProperty.AircraftAddress)).Returns(true);
            var activeFlightApiWrapper = new Mock<IActiveFlightApiWrapper>();
            var assessor = new LookupEligibilityAssessor(
                historicalFlightApiWrapper.Object,
                activeFlightApiWrapper.Object,
                _factory,
                false);

            var result = await assessor.IsEligibleForLookupAsync(ApiEndpointType.HistoricalFlights, ValidAddress);
            Assert.IsTrue(result.Eligible);
            Assert.IsTrue(result.Requeue);
        }

        [TestMethod]
        public async Task HistoricalLookupIgnoreTrackingStatusTestAsync()
        {
            var historicalFlightApiWrapper = new Mock<IHistoricalFlightApiWrapper>();
            historicalFlightApiWrapper.Setup(x => x.SupportsLookupBy(ApiProperty.AircraftAddress)).Returns(false);
            var activeFlightApiWrapper = new Mock<IActiveFlightApiWrapper>();
            var assessor = new LookupEligibilityAssessor(
                historicalFlightApiWrapper.Object,
                activeFlightApiWrapper.Object,
                _factory,
                true);

            var result = await assessor.IsEligibleForLookupAsync(ApiEndpointType.HistoricalFlights, ValidAddress);
            Assert.IsTrue(result.Eligible);
            Assert.IsTrue(result.Requeue);
        }

        [TestMethod]
        public async Task HistoricalLookupDoNotIgnoreTrackingStatusTestAsync()
        {
            var historicalFlightApiWrapper = new Mock<IHistoricalFlightApiWrapper>();
            historicalFlightApiWrapper.Setup(x => x.SupportsLookupBy(ApiProperty.AircraftAddress)).Returns(false);
            var activeFlightApiWrapper = new Mock<IActiveFlightApiWrapper>();

            var assessor = new LookupEligibilityAssessor(
                historicalFlightApiWrapper.Object,
                activeFlightApiWrapper.Object,
                _factory,
                false);

            await _factory.TrackedAircraftWriter.WriteAsync(new()
            {
                Address = ValidAddress,
                Callsign = Callsign,
                Status = TrackingStatus.Active,
                LookupAttempts = 0,
                LookupTimestamp = null
            });

            await _factory.FlightIATACodeMappingManager.AddAsync("", "", "", "", "", "", AirportType.Arrival, "", "", "BA188", Callsign, "");

            var result = await assessor.IsEligibleForLookupAsync(ApiEndpointType.HistoricalFlights, ValidAddress);
            Assert.IsTrue(result.Eligible);
            Assert.IsTrue(result.Requeue);
        }

        [TestMethod]
        public async Task HistoricalLookupNotACandidateTestAsync()
        {
            var historicalFlightApiWrapper = new Mock<IHistoricalFlightApiWrapper>();
            historicalFlightApiWrapper.Setup(x => x.SupportsLookupBy(ApiProperty.AircraftAddress)).Returns(false);
            var activeFlightApiWrapper = new Mock<IActiveFlightApiWrapper>();

            var assessor = new LookupEligibilityAssessor(
                historicalFlightApiWrapper.Object,
                activeFlightApiWrapper.Object,
                _factory,
                false);

            await _factory.TrackedAircraftWriter.WriteAsync(new()
            {
                Address = ValidAddress,
                Status = TrackingStatus.Active,
                LookupAttempts = 0,
                LookupTimestamp = null
            });

            var result = await assessor.IsEligibleForLookupAsync(ApiEndpointType.HistoricalFlights, ValidAddress);
            Assert.IsFalse(result.Eligible);
            Assert.IsTrue(result.Requeue);
        }

        [TestMethod]
        public async Task HistoricalLookupNoCallsignMappingTestAsync()
        {
            var historicalFlightApiWrapper = new Mock<IHistoricalFlightApiWrapper>();
            historicalFlightApiWrapper.Setup(x => x.SupportsLookupBy(ApiProperty.AircraftAddress)).Returns(false);
            var activeFlightApiWrapper = new Mock<IActiveFlightApiWrapper>();

            var assessor = new LookupEligibilityAssessor(
                historicalFlightApiWrapper.Object,
                activeFlightApiWrapper.Object,
                _factory,
                false);

            await _factory.TrackedAircraftWriter.WriteAsync(new()
            {
                Address = ValidAddress,
                Callsign = Callsign,
                Status = TrackingStatus.Active,
                LookupAttempts = 0,
                LookupTimestamp = null
            });

            var result = await assessor.IsEligibleForLookupAsync(ApiEndpointType.HistoricalFlights, ValidAddress);
            Assert.IsFalse(result.Eligible);
            Assert.IsFalse(result.Requeue);
        }
    }
}
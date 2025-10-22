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
    public class ActiveLookupEligibilityAssessorTest
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
        public async Task ActiveLookupByInvalidAddressTestAsync()
        {
            var activeFlightApiWrapper = new Mock<IActiveFlightApiWrapper>();
            activeFlightApiWrapper.Setup(x => x.SupportsLookupBy(ApiProperty.AircraftAddress)).Returns(true);
            var historicalFlightApiWrapper = new Mock<IHistoricalFlightApiWrapper>();

            var assessor = new LookupEligibilityAssessor(
                historicalFlightApiWrapper.Object,
                activeFlightApiWrapper.Object,
                _factory,
                false);

            var result = await assessor.IsEligibleForLookupAsync(ApiEndpointType.ActiveFlights, InvalidAddress);
            Assert.IsFalse(result.Eligible);
            Assert.IsFalse(result.Requeue);
        }

        [TestMethod]
        public async Task ActiveLookupByExcludedAddressTestAsync()
        {
            var activeFlightApiWrapper = new Mock<IActiveFlightApiWrapper>();
            activeFlightApiWrapper.Setup(x => x.SupportsLookupBy(ApiProperty.AircraftAddress)).Returns(true);
            var historicalFlightApiWrapper = new Mock<IHistoricalFlightApiWrapper>();

            await _factory.ExcludedAddressManager.AddAsync(ValidAddress);

            var assessor = new LookupEligibilityAssessor(
                historicalFlightApiWrapper.Object,
                activeFlightApiWrapper.Object,
                _factory,
                false);

            var result = await assessor.IsEligibleForLookupAsync(ApiEndpointType.ActiveFlights, ValidAddress);
            Assert.IsFalse(result.Eligible);
            Assert.IsFalse(result.Requeue);
        }

        [TestMethod]
        public async Task ActiveLookupByAddressTestAsync()
        {
            var activeFlightApiWrapper = new Mock<IActiveFlightApiWrapper>();
            activeFlightApiWrapper.Setup(x => x.SupportsLookupBy(ApiProperty.AircraftAddress)).Returns(true);
            var historicalFlightApiWrapper = new Mock<IHistoricalFlightApiWrapper>();

            var assessor = new LookupEligibilityAssessor(
                historicalFlightApiWrapper.Object,
                activeFlightApiWrapper.Object,
                _factory,
                false);

            var result = await assessor.IsEligibleForLookupAsync(ApiEndpointType.ActiveFlights, ValidAddress);
            Assert.IsTrue(result.Eligible);
            Assert.IsTrue(result.Requeue);
        }

        [TestMethod]
        public async Task ActiveLookupIgnoreTrackingStatusTestAsync()
        {
            var activeFlightApiWrapper = new Mock<IActiveFlightApiWrapper>();
            activeFlightApiWrapper.Setup(x => x.SupportsLookupBy(ApiProperty.AircraftAddress)).Returns(false);
            var historicalFlightApiWrapper = new Mock<IHistoricalFlightApiWrapper>();

            var assessor = new LookupEligibilityAssessor(
                historicalFlightApiWrapper.Object,
                activeFlightApiWrapper.Object,
                _factory,
                true);

            var result = await assessor.IsEligibleForLookupAsync(ApiEndpointType.ActiveFlights, ValidAddress);
            Assert.IsTrue(result.Eligible);
            Assert.IsTrue(result.Requeue);
        }

        [TestMethod]
        public async Task ActiveLookupDoNotIgnoreTrackingStatusTestAsync()
        {
            var activeFlightApiWrapper = new Mock<IActiveFlightApiWrapper>();
            activeFlightApiWrapper.Setup(x => x.SupportsLookupBy(ApiProperty.AircraftAddress)).Returns(false);
            var historicalFlightApiWrapper = new Mock<IHistoricalFlightApiWrapper>();

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

            var result = await assessor.IsEligibleForLookupAsync(ApiEndpointType.ActiveFlights, ValidAddress);
            Assert.IsTrue(result.Eligible);
            Assert.IsTrue(result.Requeue);
        }

        [TestMethod]
        public async Task ActiveLookupNotACandidateTestAsync()
        {
            var activeFlightApiWrapper = new Mock<IActiveFlightApiWrapper>();
            activeFlightApiWrapper.Setup(x => x.SupportsLookupBy(ApiProperty.AircraftAddress)).Returns(false);
            var historicalFlightApiWrapper = new Mock<IHistoricalFlightApiWrapper>();

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

            var result = await assessor.IsEligibleForLookupAsync(ApiEndpointType.ActiveFlights, ValidAddress);
            Assert.IsFalse(result.Eligible);
            Assert.IsTrue(result.Requeue);
        }

        [TestMethod]
        public async Task ActiveLookupNoCallsignMappingTestAsync()
        {
            var activeFlightApiWrapper = new Mock<IActiveFlightApiWrapper>();
            activeFlightApiWrapper.Setup(x => x.SupportsLookupBy(ApiProperty.AircraftAddress)).Returns(false);
            var historicalFlightApiWrapper = new Mock<IHistoricalFlightApiWrapper>();

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

            var result = await assessor.IsEligibleForLookupAsync(ApiEndpointType.ActiveFlights, ValidAddress);
            Assert.IsFalse(result.Eligible);
            Assert.IsFalse(result.Requeue);
        }
    }
}
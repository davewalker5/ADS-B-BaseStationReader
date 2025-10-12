using BaseStationReader.BusinessLogic.Api;
using BaseStationReader.BusinessLogic.Database;
using BaseStationReader.Data;
using BaseStationReader.Entities.Api;
using BaseStationReader.Entities.Tracking;
using BaseStationReader.Interfaces.Api;
using BaseStationReader.Interfaces.Database;
using BaseStationReader.Tests.Mocks;

namespace BaseStationReader.Tests.API.Wrapper
{
    [TestClass]
    public class FlightNumberApiWrapperTest
    {
        private const string AircraftAddress = "4CA216";
        private const string AirlineIATA = "EI";
        private const string AirlineICAO = "EIN";
        private const string AirlineName = "Aer Lingus";
        private const string FlightIATA = "EI527";
        private const string AirportICAO = "EGLL";
        private const string AirportIATA = "LHR";
        private const string AirportName = "London Heathrow";
        private const string Callsign = "EIN5KM";
        private const string UnmappedCallsign = "Unmapped";

        private IDatabaseManagementFactory _factory;
        private IFlightNumberApiWrapper _wrapper;

        [TestInitialize]
        public async Task InitialiseAsync()
        {
            // Create a database management factory
            var logger = new MockFileLogger();
            var context = BaseStationReaderDbContextFactory.CreateInMemoryDbContext();
            _factory = new DatabaseManagementFactory(logger, context, 0, 0);

            // Add a callsign/flight number mapping
            await _factory.FlightNumberMappingManager.AddAsync(
                AirlineICAO,
                AirlineIATA,
                AirlineName,
                AirportICAO,
                AirportIATA,
                AirportName,
                AirportType.Unknown,
                FlightIATA,
                Callsign,
                "");

            // Create the flight number wrapper
            _wrapper = new FlightNumberApiWrapper(logger, _factory);
        }

        [TestMethod]
        public async Task GetFlightNumberFromCallsignTestAsync()
        {
            var now = DateTime.Now;
            var flightNumber = await _wrapper.GetFlightNumberFromCallsignAsync(Callsign, now);

            Assert.IsNotNull(flightNumber);
            Assert.AreEqual(Callsign, flightNumber.Callsign);
            Assert.AreEqual(FlightIATA, flightNumber.Number);
            Assert.AreEqual(now, flightNumber.Date);
        }

        [TestMethod]
        public async Task GetFlightNumberFromUnmappedCallsignTestAsync()
        {
            var now = DateTime.Now;
            var flightNumber = await _wrapper.GetFlightNumberFromCallsignAsync(UnmappedCallsign, now);

            Assert.IsNotNull(flightNumber);
            Assert.AreEqual(UnmappedCallsign, flightNumber.Callsign);
            Assert.IsNull(flightNumber.Number);
            Assert.AreEqual(now, flightNumber.Date);
        }

        [TestMethod]
        public async Task GetFlightNumbersFromCallsignsTestAsync()
        {
            var now = DateTime.Now;
            var flightNumbers = await _wrapper.GetFlightNumbersFromCallsignsAsync([Callsign], now);

            Assert.IsNotNull(flightNumbers);
            Assert.HasCount(1, flightNumbers);
            Assert.AreEqual(Callsign, flightNumbers[0].Callsign);
            Assert.AreEqual(FlightIATA, flightNumbers[0].Number);
            Assert.AreEqual(now, flightNumbers[0].Date);
        }

        [TestMethod]
        public async Task GetFlightNumbersFromUnmappedCallsignsTestAsync()
        {
            var now = DateTime.Now;
            var flightNumbers = await _wrapper.GetFlightNumbersFromCallsignsAsync([UnmappedCallsign], now);

            Assert.IsNotNull(flightNumbers);
            Assert.HasCount(1, flightNumbers);
            Assert.AreEqual(UnmappedCallsign, flightNumbers[0].Callsign);
            Assert.IsNull(flightNumbers[0].Number);
            Assert.AreEqual(now, flightNumbers[0].Date);
        }

        [TestMethod]
        public async Task GetFlightNumbersForTrackedAircraftTestAsync()
        {
            var now = DateTime.Now;
            await _factory.TrackedAircraftWriter.WriteAsync(new()
            {
                Address = AircraftAddress,
                Callsign = Callsign,
                LastSeen = now,
                Status = TrackingStatus.Active
            });

            var flightNumbers = await _wrapper.GetFlightNumbersForTrackedAircraftAsync([]);

            Assert.IsNotNull(flightNumbers);
            Assert.HasCount(1, flightNumbers);
            Assert.AreEqual(Callsign, flightNumbers[0].Callsign);
            Assert.AreEqual(FlightIATA, flightNumbers[0].Number);
            Assert.AreEqual(now, flightNumbers[0].Date);
        }

        [TestMethod]
        public async Task GetFlightNumbersForTrackedAircraftWithUnmappedCallsignTestAsync()
        {
            var now = DateTime.Now;
            await _factory.TrackedAircraftWriter.WriteAsync(new()
            {
                Address = AircraftAddress,
                Callsign = UnmappedCallsign,
                LastSeen = now,
                Status = TrackingStatus.Active
            });

            var flightNumbers = await _wrapper.GetFlightNumbersForTrackedAircraftAsync([]);

            Assert.IsNotNull(flightNumbers);
            Assert.HasCount(1, flightNumbers);
            Assert.AreEqual(UnmappedCallsign, flightNumbers[0].Callsign);
            Assert.IsNull(flightNumbers[0].Number);
            Assert.AreEqual(now, flightNumbers[0].Date);
        }

        [TestMethod]
        public async Task GetFlightNumbersForTrackedAircraftWithAcceptingStatusFiltersTestAsync()
        {
            var now = DateTime.Now;
            await _factory.TrackedAircraftWriter.WriteAsync(new()
            {
                Address = AircraftAddress,
                Callsign = Callsign,
                LastSeen = now,
                Status = TrackingStatus.Inactive
            });

            var flightNumbers = await _wrapper.GetFlightNumbersForTrackedAircraftAsync([TrackingStatus.Inactive]);

            Assert.IsNotNull(flightNumbers);
            Assert.HasCount(1, flightNumbers);
            Assert.AreEqual(Callsign, flightNumbers[0].Callsign);
            Assert.AreEqual(FlightIATA, flightNumbers[0].Number);
            Assert.AreEqual(now, flightNumbers[0].Date);
        }

        [TestMethod]
        public async Task GetFlightNumbersForTrackedAircraftWithRejectingStatusFiltersTestAsync()
        {
            var now = DateTime.Now;
            await _factory.TrackedAircraftWriter.WriteAsync(new()
            {
                Address = AircraftAddress,
                Callsign = Callsign,
                LastSeen = now,
                Status = TrackingStatus.Active
            });

            var flightNumbers = await _wrapper.GetFlightNumbersForTrackedAircraftAsync([TrackingStatus.Inactive]);

            Assert.IsNotNull(flightNumbers);
            Assert.HasCount(0, flightNumbers);
        }
    }
}
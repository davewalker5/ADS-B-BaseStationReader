using System.Globalization;
using BaseStationReader.Api.Wrapper;
using BaseStationReader.BusinessLogic.Database;
using BaseStationReader.Data;
using BaseStationReader.Entities.Api;
using BaseStationReader.Entities.Config;
using BaseStationReader.Entities.Tracking;
using BaseStationReader.Interfaces.Api;
using BaseStationReader.Interfaces.Database;
using BaseStationReader.Tests.Mocks;
using Moq;

namespace BaseStationReader.Tests.API.Wrapper
{
    [TestClass]
    public class IncorrectlyRegisteredApiTest
    {
        private const string AircraftAddress = "4074B6";
        private const string Callsign = "EZY12ND";
        private const string FlightIATA = "U22123";
        private const string Embarkation = "MAN";
        private const string Destination = "FCO";
        private const string DepartureTime = "2025-09-25 07:45Z";
        private const string ModelICAO = "A320";
        private const string ModelIATA = "32A";
        private const string ModelName = "320-251N";
        private const string ManufacturerName = "Airbus";

        private IDatabaseManagementFactory _factory;

        private readonly ExternalApiSettings _settings = new()
        {
            ApiServices = [
                new ApiService() { Service = ApiServiceType.AeroDataBox, Key = "an-api-key"}
            ],
            ApiEndpoints = [
                new ApiEndpoint() { Service = ApiServiceType.AeroDataBox, EndpointType = ApiEndpointType.Aircraft, Url = "http://some.host.com/endpoint"},
                new ApiEndpoint() { Service = ApiServiceType.AeroDataBox, EndpointType = ApiEndpointType.HistoricalFlights, Url = "http://some.host.com/endpoint"}
            ]
        };

        [TestInitialize]
        public async Task InitialiseAsync()
        {
            // Create a factory that can be used to query the objects that are created during lookup
            var logger = new MockFileLogger();
            var context = BaseStationReaderDbContextFactory.CreateInMemoryDbContext();
            _factory = new DatabaseManagementFactory(logger, context, 0, 0);

            // Create all the data that would allow the call to succeed, if it weren't for the mis-configuration - this
            // means the lookup fails only because of that mis-confguration

            // Create a tracked aircraft that will match the first flight in the flights response
            DateTime.TryParse(DepartureTime, null, DateTimeStyles.AdjustToUniversal, out DateTime utc);
            _ = await _factory.TrackedAircraftWriter.WriteAsync(new()
            {
                Address = AircraftAddress,
                Callsign = Callsign,
                LastSeen = utc.AddMinutes(30).ToLocalTime(),
                Status = TrackingStatus.Active
            });

            // Add a callsign/flight IATA code mapping and a tracked aircraft with that callsign
            await _factory.FlightIATACodeMappingManager.AddAsync("", "", "", "", "", "", AirportType.Unknown, Embarkation, Destination, FlightIATA, Callsign, "");

            // Create the model and manufacturer in the database so they'll be picked up during the aircraft
            // lookup
            var manufacturer = await _factory.ManufacturerManager.AddAsync(ManufacturerName);
            await _factory.ModelManager.AddAsync(ModelIATA, ModelICAO, ModelName, manufacturer.Id);
        }

        [TestMethod]
        public async Task MisRegisteredHistoricalFlightApiTestAsync()
        {
            // Construct the wrapper with mis-registered API
            var api = new Mock<IAirportWeatherApiWrapper>();
            var register = new ExternalApiRegister(_factory.Logger);
            register.RegisterExternalApi(ApiEndpointType.HistoricalFlights, api.Object);
            var wrapper = new HistoricalFlightApiWrapper(register, null, _factory);

            // Construct the requests
            var request = new ApiLookupRequest()
            {
                FlightEndpointType = ApiEndpointType.HistoricalFlights,
                AircraftAddress = AircraftAddress,
                DepartureAirportCodes = null,
                ArrivalAirportCodes = null,
                CreateSighting = true
            };

            // Test each method in turn for a default/null response
            Assert.IsFalse(wrapper.SupportsLookupBy(ApiProperty.AircraftAddress));
            Assert.IsFalse(wrapper.SupportsLookupBy(ApiProperty.FlightIATA));
            Assert.IsNull(await wrapper.LookupFlightAsync(request));
        }

        [TestMethod]
        public async Task MisRegisteredActiveFlightApiTestAsync()
        {
            // Construct the wrapper with mis-registered API
            var api = new Mock<IAirportWeatherApiWrapper>();
            var register = new ExternalApiRegister(_factory.Logger);
            register.RegisterExternalApi(ApiEndpointType.HistoricalFlights, api.Object);
            var wrapper = new ActiveFlightApiWrapper(register, null, _factory);

            // Construct the requests
            var request = new ApiLookupRequest()
            {
                FlightEndpointType = ApiEndpointType.ActiveFlights,
                AircraftAddress = AircraftAddress,
                DepartureAirportCodes = null,
                ArrivalAirportCodes = null,
                CreateSighting = true
            };

            // Test each method in turn for a default/null response
            Assert.IsFalse(wrapper.SupportsLookupBy(ApiProperty.AircraftAddress));
            Assert.IsFalse(wrapper.SupportsLookupBy(ApiProperty.FlightIATA));
            Assert.IsNull(await wrapper.LookupFlightAsync(request));
        }
    }
}
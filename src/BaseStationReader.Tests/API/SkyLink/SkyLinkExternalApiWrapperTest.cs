using BaseStationReader.Api.Wrapper;
using BaseStationReader.BusinessLogic.Database;
using BaseStationReader.Data;
using BaseStationReader.Entities.Api;
using BaseStationReader.Entities.Config;
using BaseStationReader.Entities.Tracking;
using BaseStationReader.Interfaces.Api;
using BaseStationReader.Interfaces.Database;
using BaseStationReader.Tests.Mocks;

namespace BaseStationReader.Tests.API
{
    [TestClass]
    public class ASkyLinkExternalApiWrapperTest
    {
        private const string AircraftAddress = "4CA216";
        private const string AircraftRegistration = "EI-DEH";
        private const string ModelICAO = "A320";
        private const string ModelIATA = "32A";
        private const string ModelName = "Airbus A320 (sharklets)";
        private const string ManufacturerName = "Airbus";
        private const string Embarkation = "CDG";
        private const string Destination = "DUB";
        private const string AirlineIATA = "EI";
        private const string AirlineICAO = "EIN";
        private const string AirlineName = "Aer Lingus";
        private const string FlightIATA = "EI527";
        private const string AirportICAO = "EGLL";
        private const string AirportIATA = "LHR";
        private const string AirportName = "London Heathrow";
        private const string Callsign = "EIN5KM";
        private const string METAR = "METAR EGLL 031150Z COR AUTO 19011KT 150V240 9999 BKN005 OVC009 17/16 Q1009 NOSIG";
        private const string TAF = "TAF EGLL 021702Z 0218/0324 19012KT 9999 FEW025 PROB30 TEMPO 0220/0303 18015G25KT TEMPO 0223/0305 7000 RA PROB40 TEMPO 0300/0305 3000 +RA BKN012 BECMG 0302/0306 BKN005 TEMPO 0305/0312 6000 -RADZ PROB30 TEMPO 0305/0310 3000 DZ BKN002 BECMG 0312/0315 SCT020 PROB40 TEMPO 0312/0318 20015G25KT 8000 -RA BKN009 BECMG 0318/0320 21018G28KT TEMPO 0318/0324 4000 RADZ BKN009";
        private const string FlightResponse = "{ \"flight_number\": \"EI527\", \"status\": \"Departed 17:14\", \"airline\": \"Aer Lingus\", \"departure\": { \"airport\": \"CDG • Paris\", \"airport_full\": \"Paris Charles de Gaulle Airport\", \"scheduled_time\": \"16:55\", \"scheduled_date\": \"04 Oct\", \"actual_time\": \"17:14\", \"actual_date\": \"04 Oct\", \"terminal\": \"1\", \"gate\": \"--\", \"checkin\": \"--\" }, \"arrival\": { \"airport\": \"DUB • Dublin\", \"airport_full\": \"Dublin  International Airport\", \"scheduled_time\": \"17:40\", \"scheduled_date\": \"04 Oct\", \"estimated_time\": \"17:35\", \"estimated_date\": \"04 Oct\", \"terminal\": \"2\", \"gate\": \"--\", \"baggage\": \"--\" } }";
        private const string AirlineResponse = "[ { \"id\": 837, \"name\": \"Aer Lingus\", \"alias\": null, \"iata\": \"EI\", \"icao\": \"EIN\", \"callsign\": \"SHAMROCK\", \"country\": \"Ireland\", \"active\": \"Y\", \"logo\": \"https://media.skylinkapi.com/logos/EI.png\" } ]";
        private const string AircraftResponse = "{ \"aircraft\": [ { \"icao24\": \"4CA216\", \"callsign\": \"EIN5KM\", \"latitude\": 51.407776, \"longitude\": -0.606781, \"altitude\": 36000.0, \"ground_speed\": 399.889984, \"track\": 297.718506, \"vertical_rate\": -64.0, \"is_on_ground\": false, \"last_seen\": \"2025-10-04T15:48:39.731534\", \"first_seen\": \"2025-10-04T15:14:33.662057\", \"registration\": \"EI-DEH\", \"aircraft_type\": \"A320\", \"airline\": \"Aer Lingus\" } ], \"total_count\": 1, \"timestamp\": \"2025-10-04T15:48:44.717122\" }";
        private const string MetarResponse = "{ \"raw\": \"METAR EGLL 031150Z COR AUTO 19011KT 150V240 9999 BKN005 OVC009 17/16 Q1009 NOSIG\", \"icao\": \"EGLL\", \"airport_name\": \"London Heathrow Airport\", \"timestamp\": \"2025-10-03T12:23:39.122722Z\" }";
        private const string TafResponse = "{ \"raw\": \"TAF EGLL 021702Z 0218/0324 19012KT 9999 FEW025 PROB30 TEMPO 0220/0303 18015G25KT TEMPO 0223/0305 7000 RA PROB40 TEMPO 0300/0305 3000 +RA BKN012 BECMG 0302/0306 BKN005 TEMPO 0305/0312 6000 -RADZ PROB30 TEMPO 0305/0310 3000 DZ BKN002 BECMG 0312/0315 SCT020 PROB40 TEMPO 0312/0318 20015G25KT 8000 -RA BKN009 BECMG 0318/0320 21018G28KT TEMPO 0318/0324 4000 RADZ BKN009\", \"icao\": \"EGLL\", \"airport_name\": \"London Heathrow Airport\", \"timestamp\": \"2025-10-02T19:48:58.316421Z\" }";

        private MockTrackerHttpClient _client;
        private IExternalApiWrapper _wrapper;
        private IDatabaseManagementFactory _factory;

        private readonly ExternalApiSettings _settings = new()
        {
            ApiServices = [
                new ApiService() { Service = ApiServiceType.SkyLink, Key = "an-api-key"}
            ],
            ApiEndpoints = [
                new ApiEndpoint() { Service = ApiServiceType.SkyLink, EndpointType = ApiEndpointType.Aircraft, Url = "http://some.host.com/endpoint"},
                new ApiEndpoint() { Service = ApiServiceType.SkyLink, EndpointType = ApiEndpointType.Airlines, Url = "http://some.host.com/endpoint"},
                new ApiEndpoint() { Service = ApiServiceType.SkyLink, EndpointType = ApiEndpointType.METAR, Url = "http://some.host.com/endpoint"},
                new ApiEndpoint() { Service = ApiServiceType.SkyLink, EndpointType = ApiEndpointType.TAF, Url = "http://some.host.com/endpoint"}
            ]
        };

        [TestInitialize]
        public async Task InitialiseAsync()
        {
            // Create a factory that can be used to query the objects that are created during lookup
            var logger = new MockFileLogger();
            var context = BaseStationReaderDbContextFactory.CreateInMemoryDbContext();
            _factory = new DatabaseManagementFactory(logger, context, 0, 0);

            _client = new();
            _wrapper = new ExternalApiFactory().GetWrapperInstance(_client, _factory, ApiServiceType.SkyLink, ApiEndpointType.Flights, _settings);

            // Create a tracked aircraft that will match the first flight in the flights response
            _ = await _factory.TrackedAircraftWriter.WriteAsync(new()
            {
                Address = AircraftAddress,
                Callsign = Callsign,
                LastSeen = DateTime.Today,
                Status = TrackingStatus.Active
            });

            // Add the flight IATA code mapping
            _ = await _factory.FlightIATACodeMappingManager.AddAsync(
                AirlineICAO,
                AirlineIATA,
                AirlineName,
                AirportICAO,
                AirportIATA,
                AirportName,
                AirportType.Unknown,
                Embarkation,
                Destination,
                FlightIATA,
                Callsign, "");

            // Create the model and manufacturer in the database so they'll be picked up during the aircraft
            // lookup
            var manufacturer = await _factory.ManufacturerManager.AddAsync(ManufacturerName);
            await _factory.ModelManager.AddAsync(ModelIATA, ModelICAO, ModelName, manufacturer.Id);
        }

        [TestMethod]
        public async Task LookupTestAsync()
        {
            _client.AddResponse(AircraftResponse);
            _client.AddResponse(FlightResponse);
            _client.AddResponse(AirlineResponse);

            var request = new ApiLookupRequest()
            {
                AircraftAddress = AircraftAddress,
                DepartureAirportCodes = null,
                ArrivalAirportCodes = null,
                CreateSighting = true
            };

            var result = await _wrapper.LookupAsync(request);

            Assert.IsTrue(result.Successful);
            Assert.IsFalse(result.Requeue);
            await AssertExpectedAircraftCreatedAsync();
            await AssertExpectedAirlineCreatedAsync();
            await AssertExpectedFlightCreatedAsync();
        }

        [TestMethod]
        public async Task LookupWithAcceptingAirportFiltersTestAsync()
        {
            _client.AddResponse(AircraftResponse);
            _client.AddResponse(FlightResponse);
            _client.AddResponse(AirlineResponse);

            var request = new ApiLookupRequest()
            {
                AircraftAddress = AircraftAddress,
                DepartureAirportCodes = [Embarkation],
                ArrivalAirportCodes = [Destination],
                CreateSighting = true
            };

            var result = await _wrapper.LookupAsync(request);

            Assert.IsTrue(result.Successful);
            Assert.IsFalse(result.Requeue);
        }

        [TestMethod]
        public async Task LookupWithExcludingAirportFiltersTestAsync()
        {
            _client.AddResponse(AircraftResponse);
            _client.AddResponse(FlightResponse);
            _client.AddResponse(AirlineResponse);

            var request = new ApiLookupRequest()
            {
                AircraftAddress = AircraftAddress,
                DepartureAirportCodes = [Destination],
                ArrivalAirportCodes = [Embarkation],
                CreateSighting = true
            };

            var result = await _wrapper.LookupAsync(request);

            Assert.IsFalse(result.Successful);
            Assert.IsFalse(result.Requeue);
        }

        [TestMethod]
        public async Task LookupWithExcludingDepartureAirportFiltersTestAsync()
        {
            _client.AddResponse(AircraftResponse);
            _client.AddResponse(FlightResponse);
            _client.AddResponse(AirlineResponse);

            var request = new ApiLookupRequest()
            {
                AircraftAddress = AircraftAddress,
                DepartureAirportCodes = [Destination],
                ArrivalAirportCodes = [Destination],
                CreateSighting = true
            };

            var result = await _wrapper.LookupAsync(request);

            Assert.IsFalse(result.Successful);
            Assert.IsFalse(result.Requeue);
        }

        [TestMethod]
        public async Task LookupWithExcludingArrivalAirportFiltersTestAsync()
        {
            _client.AddResponse(AircraftResponse);
            _client.AddResponse(FlightResponse);
            _client.AddResponse(AirlineResponse);

            var request = new ApiLookupRequest()
            {
                AircraftAddress = AircraftAddress,
                DepartureAirportCodes = [Embarkation],
                ArrivalAirportCodes = [Embarkation],
                CreateSighting = true
            };

            var result = await _wrapper.LookupAsync(request);

            Assert.IsFalse(result.Successful);
            Assert.IsFalse(result.Requeue);
        }

        [TestMethod]
        public void GetCurrentWeatherTest()
        {
            _client.AddResponse(MetarResponse);
            var results = Task.Run(() => _wrapper.LookupCurrentAirportWeatherAsync(AirportICAO)).Result;

            Assert.IsNotNull(results);
            Assert.HasCount(1, results);
            Assert.AreEqual(METAR, results.First());
        }

        [TestMethod]
        public void GetWeatherForecastTest()
        {
            _client.AddResponse(TafResponse);
            var results = Task.Run(() => _wrapper.LookupAirportWeatherForecastAsync(AirportICAO)).Result;

            Assert.IsNotNull(results);
            Assert.HasCount(1, results);
            Assert.AreEqual(TAF, results.First());
        }

        private async Task AssertExpectedAircraftCreatedAsync()
        {
            var aircraft = await _factory.AircraftManager.ListAsync(x => true);

            Assert.IsNotNull(aircraft);
            Assert.HasCount(1, aircraft);
            Assert.AreEqual(AircraftAddress, aircraft[0].Address);
            Assert.AreEqual(AircraftRegistration, aircraft[0].Registration);
            Assert.AreEqual(ModelIATA, aircraft[0].Model.IATA);
            Assert.AreEqual(ModelICAO, aircraft[0].Model.ICAO);
            Assert.AreEqual(ModelName, aircraft[0].Model.Name);
            Assert.AreEqual(ManufacturerName, aircraft[0].Model.Manufacturer.Name);
        }

        private async Task AssertExpectedAirlineCreatedAsync()
        {
            var airlines = await _factory.AirlineManager.ListAsync(x => true);

            Assert.IsNotNull(airlines);
            Assert.HasCount(1, airlines);
            Assert.AreEqual(AirlineIATA, airlines[0].IATA);
            Assert.AreEqual(AirlineICAO, airlines[0].ICAO);
            Assert.AreEqual(AirlineName, airlines[0].Name);
        }

        private async Task AssertExpectedFlightCreatedAsync()
        {
            var flights = await _factory.FlightManager.ListAsync(x => true);

            Assert.IsNotNull(flights);
            Assert.HasCount(1, flights);
            Assert.AreEqual(FlightIATA, flights[0].IATA);
            Assert.AreEqual(AirlineICAO, flights[0].Airline.ICAO);
            Assert.AreEqual(AirlineIATA, flights[0].Airline.IATA);
            Assert.AreEqual(AirlineName, flights[0].Airline.Name);
        }
    }
}
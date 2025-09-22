using BaseStationReader.BusinessLogic.Api;
using BaseStationReader.BusinessLogic.Api.AirLabs;
using BaseStationReader.BusinessLogic.Configuration;
using BaseStationReader.Data;
using BaseStationReader.Entities.Config;
using BaseStationReader.Entities.Interfaces;
using BaseStationReader.Entities.Logging;

namespace BaseStationReader.Lookup.Logic
{
    internal class LookupManager
    {
        private static char[] _separators = [' ', '.'];

        private readonly LookupToolApplicationSettings _settings;
        private readonly ITrackerLogger _logger;
        private readonly BaseStationReaderDbContext _context;
        private readonly LookupToolCommandLineParser _parser;

        public LookupManager(
            LookupToolApplicationSettings settings,
            ITrackerLogger logger,
            BaseStationReaderDbContext context,
            LookupToolCommandLineParser parser)
        {
            _settings = settings;
            _logger = logger;
            _context = context;
            _parser = parser;       
        }

        public async Task Lookup()
        {
            // Extract the endpoint URLs and API ket from the application settings
            var airlinesEndpointUrl = _settings.ApiEndpoints.First(x => x.EndpointType == ApiEndpointType.Airlines).Url;
            var aircraftEndpointUrl = _settings.ApiEndpoints.First(x => x.EndpointType == ApiEndpointType.Aircraft).Url;
            var flightsEndpointUrl = _settings.ApiEndpoints.First(x => x.EndpointType == ApiEndpointType.ActiveFlights).Url;
            var key = _settings.ApiServiceKeys.First(x => x.Service == ApiServiceType.AirLabs).Key;

            // Construct the API wrapper
            var client = TrackerHttpClient.Instance;
            var wrapper = new AirLabsApiWrapper(_logger, client, _context, airlinesEndpointUrl, aircraftEndpointUrl, flightsEndpointUrl, key);

            // Extract the aircraft address and filtering properties from the command line arguments
            var address = _parser.GetValues(CommandLineOptionType.AircraftAddress)[0];
            var departureAirports = GetAirportList(CommandLineOptionType.Departure);
            var arrivalAirports = GetAirportList(CommandLineOptionType.Arrival);

            // Lookup the flight
            var flight = await wrapper.LookupAndStoreFlightAsync(address, departureAirports, arrivalAirports);
            if (flight != null)
            {
                // Lookup the aircraft, but only if the flight was found/returned. The flight
                // could be filtered out, in which case we don't want to store any of the details
                await wrapper.LookupAndStoreAircraftAsync(address);
            }
        }

        /// <summary>
        /// Extract a list of airport ICAO/IATA codes from a command line argument
        /// </summary>
        /// <param name="option"></param>
        /// <returns></returns>
        private IEnumerable<string> GetAirportList(CommandLineOptionType option)
        {
            IEnumerable<string> airportCodes = null;

            // Check the specified option is specified
            if (_parser.IsPresent(option))
            {
                // Extract the airport code list and make sure it has some content
                var airportCodeList = _parser.GetValues(option)[0].Trim();
                if (!string.IsNullOrEmpty(airportCodeList))
                {
                    // Log the list and split it list into an array of airport codes
                    _logger.LogMessage(Severity.Info, $"{option} airport code filters: {airportCodeList}");
                    airportCodes = airportCodeList.Split(_separators, StringSplitOptions.RemoveEmptyEntries);
                }
            }

            return airportCodes;
        }
    }
}
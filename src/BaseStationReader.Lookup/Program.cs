
using BaseStationReader.BusinessLogic.Api;
using BaseStationReader.BusinessLogic.Api.AirLabs;
using BaseStationReader.BusinessLogic.Configuration;
using BaseStationReader.BusinessLogic.Logging;
using BaseStationReader.Data;
using BaseStationReader.Entities.Config;
using BaseStationReader.Entities.Logging;
using BaseStationReader.Lookup.Logic;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace BaseStationReader.Lookup
{
    [ExcludeFromCodeCoverage]
    public static class Program
    {
        private static char[] _separators = [' ', '.'];
        private static LookupToolCommandLineParser _parser;
        private static FileLogger _logger;

        public static async Task Main(string[] args)
        {
            // Process the command line arguments. If help's been requested, show help and exit
            _parser = new LookupToolCommandLineParser(new HelpTabulator());
            _parser.Parse(args);
            if (_parser.IsPresent(CommandLineOptionType.Help))
            {
                _parser.Help();
            }
            else
            {
                // Read the application config file
                var settings = new LookupToolSettingsBuilder().BuildSettings(_parser, "appsettings.json");

                // Configure the log file
                _logger = new FileLogger();
                _logger.Initialise(settings.LogFile, settings.MinimumLogLevel);

                // Get the version number and application title
                Assembly assembly = Assembly.GetExecutingAssembly();
                FileVersionInfo info = FileVersionInfo.GetVersionInfo(assembly.Location);
                var title = $"Aircraft Lookup Tool v{info.FileVersion}";

                // Log the startup messages
                _logger.LogMessage(Severity.Info, new string('=', 80));
                _logger.LogMessage(Severity.Info, title);

                // Make sure the latest migrations have been applied - this ensures the DB is created and in the
                // correct state if it's absent or stale on startup
                var context = new BaseStationReaderDbContextFactory().CreateDbContext([]);
                context.Database.Migrate();
                _logger.LogMessage(Severity.Debug, "Latest database migrations have been applied");

                // If an aircraft address has been supplied, look it up and store the results
                if (_parser.IsPresent(CommandLineOptionType.AircraftAddress))
                {
                    // Extract the endpoint URLs and API ket from the application settings
                    var airlinesEndpointUrl = settings.ApiEndpoints.First(x => x.EndpointType == ApiEndpointType.Airlines).Url;
                    var aircraftEndpointUrl = settings.ApiEndpoints.First(x => x.EndpointType == ApiEndpointType.Aircraft).Url;
                    var flightsEndpointUrl = settings.ApiEndpoints.First(x => x.EndpointType == ApiEndpointType.ActiveFlights).Url;
                    var key = settings.ApiServiceKeys.First(x => x.Service == ApiServiceType.AirLabs).Key;

                    // Construct the API wrapper
                    var client = TrackerHttpClient.Instance;
                    var wrapper = new AirLabsApiWrapper(_logger, client, context, airlinesEndpointUrl, aircraftEndpointUrl, flightsEndpointUrl, key);

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
            }
        }

        /// <summary>
        /// Extract a list of airport ICAO/IATA codes from a command line argument
        /// </summary>
        /// <param name="option"></param>
        /// <returns></returns>
        private static IEnumerable<string> GetAirportList(CommandLineOptionType option)
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
using BaseStationReader.BusinessLogic.Api;
using BaseStationReader.BusinessLogic.Api.AirLabs;
using BaseStationReader.BusinessLogic.Configuration;
using BaseStationReader.BusinessLogic.Database;
using BaseStationReader.BusinessLogic.Logging;
using BaseStationReader.Data;
using BaseStationReader.Entities.Config;
using BaseStationReader.Entities.Logging;
using BaseStationReader.Lookup.Logic;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Reflection;

namespace BaseStationReader.Lookup
{
    public static class Program
    {
        private static char[] _separators = [' ', '.'];
        private static readonly FileLogger _logger = new();
        private static readonly LookupToolCommandLineParser _parser = new(new HelpTabulator());

        public static async Task Main(string[] args)
        {
            // Process the command line arguments. If help's been requested, show help and exit
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
                _logger.Initialise(settings.LogFile, settings.MinimumLogLevel);

                // Get the version number and application title
                Assembly assembly = Assembly.GetExecutingAssembly();
                FileVersionInfo info = FileVersionInfo.GetVersionInfo(assembly.Location);
                var title = $"Aircraft Lookup Tool v{info.FileVersion}";

                // Show the startup messages
                Console.WriteLine(new string('=', 80));
                Console.WriteLine(title);
                Console.WriteLine($"Output will be logged to {settings.LogFile}");

                // Log the startup messages
                _logger.LogMessage(Severity.Info, new string('=', 80));
                _logger.LogMessage(Severity.Info, title);

                // Make sure the latest migrations have been applied - this ensures the DB is created and in the
                // correct state if it's absent or stale on startup
                var context = new BaseStationReaderDbContextFactory().CreateDbContext([]);
                context.Database.Migrate();
                _logger.LogMessage(Severity.Debug, "Latest database migrations have been applied");

                // If a CSV file containing airline details has been supplied, import it
                if (_parser.IsPresent(CommandLineOptionType.ImportAirlines))
                {
                    var filePath = _parser.GetValues(CommandLineOptionType.ImportAirlines)[0];
                    var airlineManager = new AirlineManager(context);
                    var airlineImporter = new AirlineImporter(airlineManager, _logger);
                    await airlineImporter.Import(filePath);
                }

                // If a CSV file containing manufacturer details has been supplied, import it
                if (_parser.IsPresent(CommandLineOptionType.ImportManufacturers))
                {
                    var filePath = _parser.GetValues(CommandLineOptionType.ImportManufacturers)[0];
                    var manufacturerManager = new ManufacturerManager(context);
                    var manufacturerImporter = new ManufacturerImporter(manufacturerManager, _logger);
                    await manufacturerImporter.Import(filePath);
                }

                // If a CSV file containing model details has been supplied, import it
                if (_parser.IsPresent(CommandLineOptionType.ImportModels))
                {
                    var filePath = _parser.GetValues(CommandLineOptionType.ImportModels)[0];
                    var manufacturerManager = new ManufacturerManager(context);
                    var modelManager = new ModelManager(context);
                    var modelImporter = new ModelImporter(manufacturerManager, modelManager, _logger);
                    await modelImporter.Import(filePath);
                }

                // If an aircraft address has been supplied, look it up and store the results
                if (_parser.IsPresent(CommandLineOptionType.AircraftAddress))
                {
                    // Extract the API configuration properties from the settings
                    var airlinesEndpointUrl = settings.ApiEndpoints.First(x => x.EndpointType == ApiEndpointType.Airlines).Url;
                    var aircraftEndpointUrl = settings.ApiEndpoints.First(x => x.EndpointType == ApiEndpointType.Aircraft).Url;
                    var flightsEndpointUrl = settings.ApiEndpoints.First(x => x.EndpointType == ApiEndpointType.ActiveFlights).Url;
                    var key = settings.ApiServiceKeys.First(x => x.Service == ApiServiceType.AirLabs).Key;

                    // Configure the API wrapper
                    var client = TrackerHttpClient.Instance;
                    var wrapper = new AirLabsApiWrapper(_logger, client, context, airlinesEndpointUrl, aircraftEndpointUrl, flightsEndpointUrl, key);

                    // Extract the lookup parameters from the command line
                    var address = _parser.GetValues(CommandLineOptionType.AircraftAddress)[0];
                    var departureAirportCodes = GetAirportCodeList(CommandLineOptionType.Departure);
                    var arrivalAirportCodes = GetAirportCodeList(CommandLineOptionType.Arrival);

                    // Perform the
                    await wrapper.LookupAsync(address, departureAirportCodes, arrivalAirportCodes, settings.CreateSightings);
                }
            }
        }

        /// <summary>
        /// Extract a list of airport ICAO/IATA codes from a comma-separated string
        /// </summary>
        /// <param name="type"></param>
        /// <param name="airportCodeList"></param>
        /// <returns></returns>
        public static IEnumerable<string> GetAirportCodeList(CommandLineOptionType option)
        {
            IEnumerable<string> airportCodes = null;

            // Check the option is specified
            if (_parser.IsPresent(option))
            {
                // Extract the comma-separated string from the command line options
                var airportCodeList = _parser.GetValues(option)[0];
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
using BaseStationReader.BusinessLogic.Api;
using BaseStationReader.BusinessLogic.Api.Wrapper;
using BaseStationReader.BusinessLogic.Configuration;
using BaseStationReader.BusinessLogic.Export;
using BaseStationReader.Entities.Config;
using BaseStationReader.Entities.Logging;
using BaseStationReader.Interfaces.Api;
using BaseStationReader.Interfaces.Database;
using BaseStationReader.Interfaces.Logging;

namespace BaseStationReader.Lookup.Logic
{
    internal class CallsignConversionHandler : CommandHandlerBase
    {
        private readonly ApiServiceType _serviceType;
        private readonly IExternalApiWrapper _wrapper;

        public CallsignConversionHandler(
            LookupToolApplicationSettings settings,
            LookupToolCommandLineParser parser,
            ITrackerLogger logger,
            IDatabaseManagementFactory factory,
            ApiServiceType serviceType) : base(settings, parser, logger, factory)
        {
            _serviceType = serviceType;
            _wrapper = ExternalApiFactory.GetWrapperInstance(Logger, TrackerHttpClient.Instance, Factory, _serviceType, ApiEndpointType.ActiveFlights, Settings, true);
        }

        /// <summary>
        /// Handle callsign conversion for a single callsign supplied on the command line
        /// </summary>
        /// <returns></returns>
        public async Task HandleForSingleCallsignAsync()
        {
            // Exctract the callsign from the command line arguments and infer a flight number
            var callsign = Parser.GetValues(CommandLineOptionType.ConvertSingle)[0];
            var flightNumber = await _wrapper.GetFlightNumberFromCallsignAsync(callsign);
            Console.WriteLine($"Callsign {callsign} => flight number {flightNumber?.Number}");
        }

        /// <summary>
        /// Handle callsign conversion for a list of callsigns in a text file suppleid on the command
        /// line and export the results
        /// </summary>
        /// <returns></returns>
        public async Task HandleForCallsignListAsync()
        {
            // Exctract the callsign file and export CSV file paths from the command line arguments
            var callsignFilePath = Parser.GetValues(CommandLineOptionType.ConvertList)[0];
            var csvFilePath = Parser.GetValues(CommandLineOptionType.ConvertList)[1];

            // Read the list of callsigns
            var callsigns = File.ReadAllLines(callsignFilePath);
            if (callsigns.Length > 0)
            {
                // Perform the conversion and export the results
                var numbers = await _wrapper.GetFlightNumbersFromCallsignsAsync(callsigns);
                new FlightNumberExporter().Export(numbers, csvFilePath);
            }
            else
            {
                Console.WriteLine($"No callsigns to convert");
            }            
        }

        /// <summary>
        /// Handle callsign conversion for currently tracked aircraft and export for tracked aircraft
        /// </summary>
        /// <returns></returns>
        public async Task HandleForTrackedAircraftAsync()
        {
            Logger.LogMessage(Severity.Info, $"Using the {_serviceType} API");

            // Get a list of callsign to flight number conversions for tracked flights
            var numbers = await _wrapper.GetFlightNumbersForTrackedAircraftAsync([]);
            if (numbers?.Count > 0)
            {
                // Exctract the CSV file path from the command line arguments and export the data
                var filePath = Parser.GetValues(CommandLineOptionType.ConvertCallsigns)[0];
                new FlightNumberExporter().Export(numbers, filePath);
            }
            else
            {
                Console.WriteLine($"No flight numbers returned");
            }
        }
    }
}
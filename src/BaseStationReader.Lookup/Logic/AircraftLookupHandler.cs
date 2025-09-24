using BaseStationReader.BusinessLogic.Api;
using BaseStationReader.BusinessLogic.Configuration;
using BaseStationReader.Data;
using BaseStationReader.Entities.Config;
using BaseStationReader.Entities.Interfaces;
using BaseStationReader.Entities.Logging;
using BaseStationReader.Entities.Lookup;

namespace BaseStationReader.Lookup.Logic
{
    internal class AircraftLookupHandler : CommandHandlerBase
    {
        private static char[] _separators = [' ', '.'];

        public AircraftLookupHandler(
            LookupToolApplicationSettings settings,
            LookupToolCommandLineParser parser,
            ITrackerLogger logger,
            BaseStationReaderDbContext context) : base(settings, parser, logger, context)
        {

        }

        /// <summary>
        /// Handle the airline import command
        /// </summary>
        /// <returns></returns>
        public override async Task Handle()
        {
            // Extract the API configuration properties from the settings
            var apiProperties = new ApiConfiguration()
            {
                DatabaseContext = Context,
                AirlinesEndpointUrl = Settings.ApiEndpoints.First(x => x.EndpointType == ApiEndpointType.Airlines).Url,
                AircraftEndpointUrl = Settings.ApiEndpoints.First(x => x.EndpointType == ApiEndpointType.Aircraft).Url,
                FlightsEndpointUrl = Settings.ApiEndpoints.First(x => x.EndpointType == ApiEndpointType.ActiveFlights).Url,
                Key = Settings.ApiServiceKeys.First(x => x.Service == ApiServiceType.AirLabs).Key
            };

            // Configure the API wrapper
            var client = TrackerHttpClient.Instance;
            var wrapper = ApiWrapperBuilder.GetInstance(Settings.LiveApi);
            if (wrapper != null)
            {
                wrapper.Initialise(Logger, client, apiProperties);

                // Extract the lookup parameters from the command line
                var address = Parser.GetValues(CommandLineOptionType.AircraftAddress)[0];
                var departureAirportCodes = GetAirportCodeList(CommandLineOptionType.Departure);
                var arrivalAirportCodes = GetAirportCodeList(CommandLineOptionType.Arrival);

                // Perform the
                await wrapper.LookupAsync(address, departureAirportCodes, arrivalAirportCodes, Settings.CreateSightings);
            }
            else
            {
                Logger.LogMessage(Severity.Error, $"Live API type is not specified or is not supported");
            }
        }

        /// <summary>
        /// Extract a list of airport ICAO/IATA codes from a comma-separated string
        /// </summary>
        /// <param name="type"></param>
        /// <param name="airportCodeList"></param>
        /// <returns></returns>
        public IEnumerable<string> GetAirportCodeList(CommandLineOptionType option)
        {
            IEnumerable<string> airportCodes = null;

            // Check the option is specified
            if (Parser.IsPresent(option))
            {
                // Extract the comma-separated string from the command line options
                var airportCodeList = Parser.GetValues(option)[0];
                if (!string.IsNullOrEmpty(airportCodeList))
                {
                    // Log the list and split it list into an array of airport codes
                    Logger.LogMessage(Severity.Info, $"{option} airport code filters: {airportCodeList}");
                    airportCodes = airportCodeList.Split(_separators, StringSplitOptions.RemoveEmptyEntries);
                }
            }

            return airportCodes;
        }
    }
}
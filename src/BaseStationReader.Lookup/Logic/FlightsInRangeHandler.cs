using BaseStationReader.BusinessLogic.Api;
using BaseStationReader.BusinessLogic.Configuration;
using BaseStationReader.BusinessLogic.Export;
using BaseStationReader.Data;
using BaseStationReader.Entities.Config;
using BaseStationReader.Entities.Interfaces;
using BaseStationReader.Entities.Logging;
using BaseStationReader.Entities.Lookup;

namespace BaseStationReader.Lookup.Logic
{
    internal class FlightsInRangeHandler : CommandHandlerBase
    {
        public FlightsInRangeHandler(
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
            // Get the search parameters from the command line
            var filePath = Parser.GetValues(CommandLineOptionType.FlightsInRange)[1];
            var rangeString = Parser.GetValues(CommandLineOptionType.FlightsInRange)[0];
            if (!int.TryParse(rangeString, out int rangeNm))
            {
                Logger.LogMessage(Severity.Error, $"'{rangeString}' is not a valid range");
                return;
            }

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
                // Initialise the wrapper
                wrapper.Initialise(Logger, client, apiProperties);

                // Perform the lookup
                var flights = await wrapper.LookupFlightsInBoundingBox(
                    Settings.ReceiverLatitude.Value,
                    Settings.ReceiverLongitude.Value,
                    rangeNm);

                // Export the data
                new FlightExporter().Export(flights, filePath);
            }
            else
            {
                Logger.LogMessage(Severity.Error, $"Live API type is not specified or is not supported");
            }
        }
    }
}
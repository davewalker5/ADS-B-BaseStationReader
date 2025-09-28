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
        private readonly ApiServiceType _serviceType;

        public FlightsInRangeHandler(
            LookupToolApplicationSettings settings,
            LookupToolCommandLineParser parser,
            ITrackerLogger logger,
            BaseStationReaderDbContext context,
            ApiServiceType serviceType) : base(settings, parser, logger, context)
        {
            _serviceType = serviceType;
        }

        /// <summary>
        /// Handle the command to lookup flights within a defined bounding box
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
                AirlinesEndpointUrl = Settings.ApiEndpoints.First(x =>
                    x.EndpointType == ApiEndpointType.Airlines && x.Service == _serviceType).Url,
                AircraftEndpointUrl = Settings.ApiEndpoints.First(x =>
                    x.EndpointType == ApiEndpointType.Aircraft && x.Service == _serviceType).Url,
                FlightsEndpointUrl = Settings.ApiEndpoints.First(x =>
                    x.EndpointType == ApiEndpointType.ActiveFlights && x.Service == _serviceType).Url,
                Key = Settings.ApiServiceKeys.First(x => x.Service == _serviceType).Key
            };

            // Configure the API wrapper
            var client = TrackerHttpClient.Instance;
            var wrapper = ApiWrapperBuilder.GetInstance(_serviceType);
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
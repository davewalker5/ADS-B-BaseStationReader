using BaseStationReader.BusinessLogic.Api;
using BaseStationReader.BusinessLogic.Configuration;
using BaseStationReader.BusinessLogic.Export;
using BaseStationReader.Entities.Config;
using BaseStationReader.Interfaces.Logging;
using BaseStationReader.Entities.Logging;
using BaseStationReader.BusinessLogic.Api.Wrapper;
using BaseStationReader.BusinessLogic.Database;

namespace BaseStationReader.Lookup.Logic
{
    internal class FlightsInRangeHandler : CommandHandlerBase
    {
        private readonly ApiServiceType _serviceType;

        public FlightsInRangeHandler(
            LookupToolApplicationSettings settings,
            LookupToolCommandLineParser parser,
            ITrackerLogger logger,
            DatabaseManagementFactory factory,
            ApiServiceType serviceType) : base(settings, parser, logger, factory)
        {
            _serviceType = serviceType;
        }

        /// <summary>
        /// Handle the command to lookup flights within a defined bounding box
        /// </summary>
        /// <returns></returns>
        public async Task HandleAsync()
        {
            // Get the search parameters from the command line
            var filePath = Parser.GetValues(CommandLineOptionType.FlightsInRange)[1];
            var rangeString = Parser.GetValues(CommandLineOptionType.FlightsInRange)[0];
            if (!int.TryParse(rangeString, out int rangeNm))
            {
                Logger.LogMessage(Severity.Error, $"'{rangeString}' is not a valid range");
                return;
            }

            Logger.LogMessage(Severity.Info, $"Using the {_serviceType} API");

            // Configure the external API wrappe
            var wrapper = ExternalApiFactory.GetWrapperInstance(Logger, TrackerHttpClient.Instance, Context, null, _serviceType, ApiEndpointType.ActiveFlights, Settings);

            // Perform the lookup
            var flights = await wrapper.LookupActiveFlightsInBoundingBoxAsync(
                Settings.ReceiverLatitude.Value,
                Settings.ReceiverLongitude.Value,
                rangeNm);

            // Export the data
            new FlightExporter().Export(flights, filePath);
        }
    }
}
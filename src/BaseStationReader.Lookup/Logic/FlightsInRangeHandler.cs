using BaseStationReader.BusinessLogic.Api;
using BaseStationReader.BusinessLogic.Configuration;
using BaseStationReader.BusinessLogic.Export;
using BaseStationReader.Data;
using BaseStationReader.Entities.Config;
using BaseStationReader.Interfaces.Logging;
using BaseStationReader.Entities.Logging;

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

            Logger.LogMessage(Severity.Info, $"Using the {_serviceType} API");

            // Configure the external API wrappe
            var wrapper = ExternalApiFactory.GetWrapperInstance(Logger, TrackerHttpClient.Instance, Context, null, _serviceType, ApiEndpointType.ActiveFlights, Settings);

            // Perform the lookup
            var flights = await wrapper.LookupActiveFlightsInBoundingBox(
                Settings.ReceiverLatitude.Value,
                Settings.ReceiverLongitude.Value,
                rangeNm);

            // Export the data
            new FlightExporter().Export(flights, filePath);
        }
    }
}
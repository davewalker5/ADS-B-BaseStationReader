using BaseStationReader.BusinessLogic.Api;
using BaseStationReader.BusinessLogic.Api.Wrapper;
using BaseStationReader.BusinessLogic.Configuration;
using BaseStationReader.BusinessLogic.Database;
using BaseStationReader.BusinessLogic.Export;
using BaseStationReader.Entities.Config;
using BaseStationReader.Entities.Logging;
using BaseStationReader.Interfaces.Database;
using BaseStationReader.Interfaces.Logging;

namespace BaseStationReader.Lookup.Logic
{
    internal class FlightNumberExportHandler : CommandHandlerBase
    {
        private readonly ApiServiceType _serviceType;

        public FlightNumberExportHandler(
            LookupToolApplicationSettings settings,
            LookupToolCommandLineParser parser,
            ITrackerLogger logger,
            IDatabaseManagementFactory factory,
            ApiServiceType serviceType) : base(settings, parser, logger, factory)
        {
            _serviceType = serviceType;
        }

        public async Task Handle()
        {
            Logger.LogMessage(Severity.Info, $"Using the {_serviceType} API");

            // Configure the external API wrapper
            var trackedAircraftWriter = new TrackedAircraftWriter(Context);
            var wrapper = ExternalApiFactory.GetWrapperInstance(Logger, TrackerHttpClient.Instance, Context, trackedAircraftWriter, _serviceType, ApiEndpointType.ActiveFlights, Settings);

            // Get a list of callsign to flight number conversions for tracked flights
            var numbers = await wrapper.GetFlightNumbersForTrackedAircraftAsync([]);
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
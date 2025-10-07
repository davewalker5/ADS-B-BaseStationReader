using BaseStationReader.BusinessLogic.Api;
using BaseStationReader.BusinessLogic.Configuration;
using BaseStationReader.Entities.Config;
using BaseStationReader.Interfaces.Logging;
using BaseStationReader.Entities.Logging;
using BaseStationReader.BusinessLogic.Api.Wrapper;
using BaseStationReader.Interfaces.Database;

namespace BaseStationReader.Lookup.Logic
{
    internal class AircraftLookupHandler : LookupHandlerBase
    {
        private readonly ApiServiceType _serviceType;

        public AircraftLookupHandler(
            LookupToolApplicationSettings settings,
            LookupToolCommandLineParser parser,
            ITrackerLogger logger,
            IDatabaseManagementFactory factory,
            ApiServiceType serviceType) : base(settings, parser, logger, factory)
        {
            _serviceType = serviceType;
        }

        /// <summary>
        /// Handle the live aircraft lookup command
        /// </summary>
        /// <returns></returns>
        public async Task Handle()
        {
            Logger.LogMessage(Severity.Info, $"Using the {_serviceType} API");

            // Configure the external API wrapper
            var wrapper = ExternalApiFactory.GetWrapperInstance(Logger, TrackerHttpClient.Instance, Context, null, _serviceType, ApiEndpointType.ActiveFlights, Settings, null);

            // Extract the lookup parameters from the command line
            var address = Parser.GetValues(CommandLineOptionType.AircraftAddress)[0];
            var departureAirportCodes = GetAirportCodeList(CommandLineOptionType.Departure);
            var arrivalAirportCodes = GetAirportCodeList(CommandLineOptionType.Arrival);

            // Perform the lookup
            await wrapper.LookupAsync(ApiEndpointType.ActiveFlights, address, departureAirportCodes, arrivalAirportCodes, Settings.CreateSightings);
        }
    }
}
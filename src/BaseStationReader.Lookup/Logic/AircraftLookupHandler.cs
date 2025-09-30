using BaseStationReader.BusinessLogic.Api;
using BaseStationReader.BusinessLogic.Configuration;
using BaseStationReader.Data;
using BaseStationReader.Entities.Config;
using BaseStationReader.Interfaces.Logging;
using BaseStationReader.Entities.Logging;

namespace BaseStationReader.Lookup.Logic
{
    internal class AircraftLookupHandler : LookupHandlerBase
    {
        private readonly ApiServiceType _serviceType;

        public AircraftLookupHandler(
            LookupToolApplicationSettings settings,
            LookupToolCommandLineParser parser,
            ITrackerLogger logger,
            BaseStationReaderDbContext context,
            ApiServiceType serviceType) : base(settings, parser, logger, context)
        {
            _serviceType = serviceType;
        }

        /// <summary>
        /// Handle the live aircraft lookup command
        /// </summary>
        /// <returns></returns>
        public override async Task Handle()
        {
            Logger.LogMessage(Severity.Info, $"Using the {_serviceType} API");

            // Configure the external API wrappe
            var wrapper = ExternalApiFactory.GetWrapperInstance(Logger, Context, null, _serviceType, ApiEndpointType.ActiveFlights, Settings);

            // Extract the lookup parameters from the command line
            var address = Parser.GetValues(CommandLineOptionType.AircraftAddress)[0];
            var departureAirportCodes = GetAirportCodeList(CommandLineOptionType.Departure);
            var arrivalAirportCodes = GetAirportCodeList(CommandLineOptionType.Arrival);

            // Perform the lookup
            await wrapper.LookupAsync(ApiEndpointType.ActiveFlights, address, departureAirportCodes, arrivalAirportCodes, Settings.CreateSightings);
        }
    }
}
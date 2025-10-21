using BaseStationReader.Api;
using BaseStationReader.BusinessLogic.Configuration;
using BaseStationReader.Entities.Config;
using BaseStationReader.Interfaces.Logging;
using BaseStationReader.Interfaces.Database;
using BaseStationReader.Entities.Tracking;
using BaseStationReader.Interfaces.Api;

namespace BaseStationReader.Lookup.Logic
{
    internal class AircraftLookupHandler : LookupHandlerBase
    {
        public AircraftLookupHandler(
            LookupToolApplicationSettings settings,
            LookupToolCommandLineParser parser,
            ITrackerLogger logger,
            IDatabaseManagementFactory factory,
            IExternalApiFactory apiFactory) : base(settings, parser, logger, factory, apiFactory)
        {
        }

        /// <summary>
        /// Handle the live aircraft lookup command
        /// </summary>
        /// <returns></returns>
        public async Task HandleAsync()
        {
            // Get an instance of the API wrapper
            var wrapper = GetWrapperInstance(Settings.LiveApi, ApiEndpointType.ActiveFlights);

            // Extract the lookup parameters from the command line
            var address = Parser.GetValues(CommandLineOptionType.AircraftAddress)[0];
            var departureAirportCodes = GetAirportCodeList(CommandLineOptionType.Departure);
            var arrivalAirportCodes = GetAirportCodeList(CommandLineOptionType.Arrival);

            // Create the lookup request
            var request = new ApiLookupRequest()
            {
                FlightEndpointType = ApiEndpointType.ActiveFlights,
                AircraftAddress = address,
                DepartureAirportCodes = departureAirportCodes,
                ArrivalAirportCodes = arrivalAirportCodes,
                CreateSighting = Settings.CreateSightings
            };

            // Perform the lookup
            await wrapper.LookupAsync(request);
        }
    }
}
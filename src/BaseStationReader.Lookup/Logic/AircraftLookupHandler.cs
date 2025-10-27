using BaseStationReader.BusinessLogic.Configuration;
using BaseStationReader.Entities.Config;
using BaseStationReader.Interfaces.Logging;
using BaseStationReader.Entities.Tracking;
using BaseStationReader.Interfaces.Database;
using BaseStationReader.Interfaces.Api;
using BaseStationReader.Entities.Logging;

namespace BaseStationReader.Lookup.Logic
{
    internal class AircraftLookupHandler: LookupHandlerBase
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
        /// Handle the airline import command
        /// </summary>
        /// <returns></returns>
        public async Task HandleAsync()
        {
            // Get an instance of the API wrapper
            var wrapper = GetWrapperInstance(Settings.FlightApi, ApiEndpointType.Flights, false);

            // Extract the lookup parameters from the command line
            var departureAirportCodes = GetAirportCodeList(CommandLineOptionType.Departure);
            var arrivalAirportCodes = GetAirportCodeList(CommandLineOptionType.Arrival);

            // Retrieve a list of aircraft that haven't been looked up yet
            var aircraft = await Factory.TrackedAircraftWriter.ListLookupCandidatesAsync();
            Logger.LogMessage(Severity.Info, $"Found {aircraft.Count} candidate(s) for lookup");

            foreach (var a in aircraft)
            {
                // Create the lookup request
                var request = new ApiLookupRequest()
                {
                    FlightEndpointType = ApiEndpointType.Flights,
                    AircraftAddress = a.Address,
                    DepartureAirportCodes = departureAirportCodes,
                    ArrivalAirportCodes = arrivalAirportCodes,
                    CreateSighting = Settings.CreateSightings
                };

                // Perform the lookup
                await wrapper.LookupAsync(request);
            }
        }
    }
}
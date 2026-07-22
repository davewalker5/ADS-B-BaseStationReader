using BaseStationReader.BusinessLogic.Configuration;
using BaseStationReader.Entities.Config;
using BaseStationReader.Interfaces.Logging;
using BaseStationReader.Entities.Tracking;
using BaseStationReader.Interfaces.Database;
using BaseStationReader.Interfaces.Api;
using BaseStationReader.Entities.Logging;

namespace BaseStationReader.Lookup.Logic
{
    internal class SightingCreationHandler : LookupHandlerBase
    {
        public SightingCreationHandler(
            LookupToolApplicationSettings settings,
            LookupToolCommandLineParser parser,
            ITrackerLogger logger,
            IDatabaseManagementFactory factory,
            IExternalApiFactory apiFactory) : base(settings, parser, logger, factory, apiFactory)
        {
        }

        /// <summary>
        /// Resolve candidate aircraft and flight details using local database records
        /// </summary>
        /// <returns></returns>
        public async Task HandleAsync()
        {
            // Get an instance of the API wrapper
            var wrapper = GetWrapperInstance(Settings.FlightApi);

            // Retrieve a list of aircraft that haven't been looked up yet
            var aircraft = await Factory.TrackedAircraftWriter.ListSightingCreationCandidatesAsync();
            Logger.LogMessage(Severity.Info, $"Found {aircraft.Count} candidate(s) for lookup");

            foreach (var a in aircraft)
            {
                // Create the lookup request
                var request = new ApiLookupRequest()
                {
                    AircraftAddress = a.Address,
                    DepartureAirportCodes = null,
                    ArrivalAirportCodes = null,
                    CreateSighting = true,
                    AllowExternalApiLookup = false
                };

                // Perform the lookup
                await wrapper.LookupAsync(request);
            }
        }
    }
}

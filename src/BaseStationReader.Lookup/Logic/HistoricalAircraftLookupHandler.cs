using BaseStationReader.BusinessLogic.Api;
using BaseStationReader.BusinessLogic.Configuration;
using BaseStationReader.BusinessLogic.Database;
using BaseStationReader.Entities.Config;
using BaseStationReader.Interfaces.Logging;
using BaseStationReader.Entities.Logging;
using BaseStationReader.BusinessLogic.Api.Wrapper;
using BaseStationReader.Entities.Tracking;

namespace BaseStationReader.Lookup.Logic
{
    internal class HistoricalAircraftLookupHandler: LookupHandlerBase
    {
        private readonly ApiServiceType _serviceType;

        public HistoricalAircraftLookupHandler(
            LookupToolApplicationSettings settings,
            LookupToolCommandLineParser parser,
            ITrackerLogger logger,
            DatabaseManagementFactory factory,
            ApiServiceType serviceType) : base(settings, parser, logger, factory)
        {
            _serviceType = serviceType;
        }

        /// <summary>
        /// Handle the airline import command
        /// </summary>
        /// <returns></returns>
        public async Task HandleAsync()
        {
            Logger.LogMessage(Severity.Info, $"Using the {_serviceType} API");

            // Configure the external API wrapper
            var wrapper = ExternalApiFactory.GetWrapperInstance(Logger, TrackerHttpClient.Instance, Factory, _serviceType, ApiEndpointType.HistoricalFlights, Settings, false);

            // Extract the lookup parameters from the command line
            var departureAirportCodes = GetAirportCodeList(CommandLineOptionType.Departure);
            var arrivalAirportCodes = GetAirportCodeList(CommandLineOptionType.Arrival);

            // Retrieve a list of aircraft that haven't been looked up yet
            var aircraft = await Factory.TrackedAircraftWriter.ListLookupCandidatesAsync();
            foreach (var a in aircraft)
            {
                // Create the lookup request
                var request = new ApiLookupRequest()
                {
                    FlightEndpointType = ApiEndpointType.HistoricalFlights,
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
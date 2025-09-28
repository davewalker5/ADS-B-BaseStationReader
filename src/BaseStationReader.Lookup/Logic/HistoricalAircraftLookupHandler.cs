using BaseStationReader.BusinessLogic.Api;
using BaseStationReader.BusinessLogic.Configuration;
using BaseStationReader.BusinessLogic.Database;
using BaseStationReader.Data;
using BaseStationReader.Entities.Config;
using BaseStationReader.Entities.Interfaces;
using BaseStationReader.Entities.Logging;
using BaseStationReader.Entities.Lookup;

namespace BaseStationReader.Lookup.Logic
{
    internal class HistoricalAircraftLookupHandler: LookupHandlerBase
    {
        private readonly TrackedAircraftWriter _writer;
        private readonly ApiServiceType _serviceType;

        public HistoricalAircraftLookupHandler(
            LookupToolApplicationSettings settings,
            LookupToolCommandLineParser parser,
            ITrackerLogger logger,
            BaseStationReaderDbContext context,
            TrackedAircraftWriter writer,
            ApiServiceType serviceType) : base(settings, parser, logger, context)
        {
            _writer = writer;
            _serviceType = serviceType;
        }

        /// <summary>
        /// Handle the airline import command
        /// </summary>
        /// <returns></returns>
        public override async Task Handle()
        {
            // Extract the API configuration properties from the settings
            var apiProperties = new ApiConfiguration()
            {
                DatabaseContext = Context,
                AircraftEndpointUrl = Settings.ApiEndpoints.First(x =>
                    x.EndpointType == ApiEndpointType.Aircraft && x.Service == _serviceType).Url,
                FlightsEndpointUrl = Settings.ApiEndpoints.First(x =>
                    x.EndpointType == ApiEndpointType.HistoricalFlights && x.Service == _serviceType).Url,
                Key = Settings.ApiServiceKeys.First(x => x.Service == _serviceType).Key
            };

            // Configure the API wrapper
            var client = TrackerHttpClient.Instance;
            var wrapper = ApiWrapperBuilder.GetInstance(_serviceType);
            if (wrapper != null)
            {
                wrapper.Initialise(Logger, client, apiProperties);

                // Extract the lookup parameters from the command line
                var address = Parser.GetValues(CommandLineOptionType.AircraftAddress)[0];
                var departureAirportCodes = GetAirportCodeList(CommandLineOptionType.Departure);
                var arrivalAirportCodes = GetAirportCodeList(CommandLineOptionType.Arrival);

                // Retrieve a list of aircraft that haven't been looked up yet
                var aircraft = await _writer.ListAsync(x => x.LookupTimestamp == null);
                foreach (var a in aircraft)
                {
                    // Look this one up then set the timestamp so it won't be considered again
                    await wrapper.LookupAsync(address, departureAirportCodes, arrivalAirportCodes, Settings.CreateSightings);
                    await _writer.SetLookupTimestamp(a.Id);
                }
            }
            else
            {
                Logger.LogMessage(Severity.Error, $"Historical API type is not specified or is not supported");
            }
        }
    }
}
using BaseStationReader.BusinessLogic.Api;
using BaseStationReader.BusinessLogic.Configuration;
using BaseStationReader.BusinessLogic.Database;
using BaseStationReader.Data;
using BaseStationReader.Entities.Config;
using BaseStationReader.Entities.Interfaces;
using BaseStationReader.Entities.Logging;

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
            Logger.LogMessage(Severity.Info, $"Using the {_serviceType} API");

            // Configure the API wrapper
            var client = TrackerHttpClient.Instance;
            var wrapper = ApiWrapperBuilder.GetInstance(Logger, Settings, Context, client, _serviceType);
            if (wrapper != null)
            {
                // Extract the lookup parameters from the command line
                var departureAirportCodes = GetAirportCodeList(CommandLineOptionType.Departure);
                var arrivalAirportCodes = GetAirportCodeList(CommandLineOptionType.Arrival);

                // Retrieve a list of aircraft that haven't been looked up yet
                var aircraft = await _writer.ListAsync(x => x.LookupTimestamp == null);
                foreach (var a in aircraft)
                {
                    // Look this one up then set the timestamp so it won't be considered again
                    await wrapper.LookupAsync(a.Address, departureAirportCodes, arrivalAirportCodes, Settings.CreateSightings);
                    await _writer.SetLookupTimestamp(a.Id);
                }
            }
        }
    }
}
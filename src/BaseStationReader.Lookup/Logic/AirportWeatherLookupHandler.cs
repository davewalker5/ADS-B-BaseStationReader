using BaseStationReader.Api;
using BaseStationReader.BusinessLogic.Configuration;
using BaseStationReader.Entities.Config;
using BaseStationReader.Interfaces.Logging;
using BaseStationReader.Entities.Logging;
using BaseStationReader.Interfaces.Database;
using BaseStationReader.Interfaces.Api;

namespace BaseStationReader.Lookup.Logic
{
    internal class AirportWeatherLookupHandler : LookupHandlerBase
    {
        private readonly ApiServiceType _serviceType;
        private readonly IExternalApiWrapper _wrapper;

        public AirportWeatherLookupHandler(
            LookupToolApplicationSettings settings,
            LookupToolCommandLineParser parser,
            ITrackerLogger logger,
            IDatabaseManagementFactory factory,
            IExternalApiFactory apiFactory,
            ApiServiceType serviceType) : base(settings, parser, logger, factory, apiFactory)
        {
            _serviceType = serviceType;
            _wrapper = ApiFactory.GetWrapperInstance(Logger, TrackerHttpClient.Instance, Factory, _serviceType, ApiEndpointType.ActiveFlights, Settings, true);
        }

        /// <summary>
        /// Handle the live airport weather lookup command
        /// </summary>
        /// <returns></returns>
        public async Task HandleMetarAsync()
        {
            Logger.LogMessage(Severity.Info, $"Using the {_serviceType} API");

            // Extract the lookup parameters from the command line
            var icao = Parser.GetValues(CommandLineOptionType.METAR)[0];

            // Perform the lookup
            var results = await _wrapper.LookupCurrentAirportWeatherAsync(icao);
            if (results?.Count() > 0)
            {
                foreach (var result in results)
                {
                    Console.WriteLine($"Current weather for {icao} : {result}");
                }
            }
            else
            {
                Console.WriteLine($"No weather results returned for {icao}");
            }
        }

        /// <summary>
        /// Handle the live airport weather lookup command
        /// </summary>
        /// <returns></returns>
        public async Task HandleTafAsync()
        {
            Logger.LogMessage(Severity.Info, $"Using the {_serviceType} API");

            // Extract the lookup parameters from the command line
            var icao = Parser.GetValues(CommandLineOptionType.TAF)[0];

            // Perform the lookup
            var results = await _wrapper.LookupAirportWeatherForecastAsync(icao);
            if (results?.Count() > 0)
            {
                foreach (var result in results)
                {
                    Console.WriteLine($"Weather forecast for {icao} : {result}");
                }
            }
            else
            {
                Console.WriteLine($"No weather forecast returned for {icao}");
            }
        }
    }
}
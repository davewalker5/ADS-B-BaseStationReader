using BaseStationReader.Api;
using BaseStationReader.BusinessLogic.Configuration;
using BaseStationReader.Entities.Config;
using BaseStationReader.Interfaces.Logging;
using BaseStationReader.Interfaces.Database;
using BaseStationReader.Interfaces.Api;

namespace BaseStationReader.Lookup.Logic
{
    internal class AirportWeatherLookupHandler : LookupHandlerBase
    {

        public AirportWeatherLookupHandler(
            LookupToolApplicationSettings settings,
            LookupToolCommandLineParser parser,
            ITrackerLogger logger,
            IDatabaseManagementFactory factory,
            IExternalApiFactory apiFactory) : base(settings, parser, logger, factory, apiFactory)
        {
        }

        /// <summary>
        /// Handle the live airport weather lookup command
        /// </summary>
        /// <returns></returns>
        public async Task HandleMetarAsync()
        {
            // Get an instance of the API wrapper
            var wrapper = GetWrapperInstance(Settings.WeatherApi);

            // Extract the lookup parameters from the command line
            var icao = Parser.GetValues(CommandLineOptionType.METAR)[0];

            // Perform the lookup
            var results = await wrapper.LookupCurrentAirportWeatherAsync(icao);
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
            // Get an instance of the API wrapper
            var wrapper = GetWrapperInstance(Settings.WeatherApi);

            // Extract the lookup parameters from the command line
            var icao = Parser.GetValues(CommandLineOptionType.TAF)[0];

            // Perform the lookup
            var results = await wrapper.LookupAirportWeatherForecastAsync(icao);
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
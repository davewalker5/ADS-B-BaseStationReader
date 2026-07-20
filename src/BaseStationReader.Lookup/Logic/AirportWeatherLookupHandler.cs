using BaseStationReader.Api;
using BaseStationReader.BusinessLogic.Configuration;
using BaseStationReader.BusinessLogic.Weather;
using BaseStationReader.Entities.Config;
using BaseStationReader.Interfaces.Logging;
using BaseStationReader.Interfaces.Database;
using BaseStationReader.Interfaces.Api;

namespace BaseStationReader.Lookup.Logic
{
    internal class AirportWeatherLookupHandler : LookupHandlerBase
    {
        /// <summary>
        /// Initialises a handler for airport weather lookup commands.
        /// </summary>
        /// <param name="settings">The lookup tool settings.</param>
        /// <param name="parser">The parsed command-line options.</param>
        /// <param name="logger">The application logger.</param>
        /// <param name="factory">The database management factory.</param>
        /// <param name="apiFactory">The external API factory.</param>
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
                    // Show the source report followed by its expanded interpretation.
                    WriteWeatherReport($"Current weather for {icao}", result, MetarDecoder.Decode);
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
                    // Show the source forecast followed by its expanded interpretation.
                    WriteWeatherReport($"Weather forecast for {icao}", result, TafDecoder.Decode);
                }
            }
            else
            {
                Console.WriteLine($"No weather forecast returned for {icao}");
            }
        }

        /// <summary>
        /// Writes a raw aviation weather report and its human-readable decoding.
        /// </summary>
        /// <param name="heading">The heading identifying the airport and report type.</param>
        /// <param name="report">The raw METAR or TAF report.</param>
        /// <param name="decode">The decoder appropriate for the report type.</param>
        private static void WriteWeatherReport(
            string heading,
            string report,
            Func<string, IReadOnlyList<string>> decode)
        {
            // Put the raw report on its own line so it remains easy to copy and compare.
            Console.WriteLine($"{heading}:");
            Console.WriteLine($"  Raw: {report}");

            try
            {
                // Indent every decoded item beneath a single, visually distinct heading.
                Console.WriteLine("  Decoded:");
                foreach (var line in decode(report))
                {
                    Console.WriteLine($"    {line}");
                }
            }
            catch (ArgumentException exception)
            {
                // A non-standard report must not prevent the raw API response being displayed.
                Console.WriteLine($"  Decoded: unavailable ({exception.Message})");
            }

            // Separate multiple reports returned by an API for readability.
            Console.WriteLine();
        }
    }
}

using BaseStationReader.BusinessLogic.Api;
using BaseStationReader.BusinessLogic.Configuration;
using BaseStationReader.Data;
using BaseStationReader.Entities.Config;
using BaseStationReader.Interfaces.Logging;
using BaseStationReader.Entities.Logging;
using BaseStationReader.BusinessLogic.Api.Wrapper;

namespace BaseStationReader.Lookup.Logic
{
    internal class AirportWeatherLookupHandler : LookupHandlerBase
    {
        private readonly ApiServiceType _serviceType;

        public AirportWeatherLookupHandler(
            LookupToolApplicationSettings settings,
            LookupToolCommandLineParser parser,
            ITrackerLogger logger,
            BaseStationReaderDbContext context,
            ApiServiceType serviceType) : base(settings, parser, logger, context)
        {
            _serviceType = serviceType;
        }

        /// <summary>
        /// Handle the live airport weather lookup command
        /// </summary>
        /// <returns></returns>
        public override async Task Handle()
        {
            Logger.LogMessage(Severity.Info, $"Using the {_serviceType} API");

            // Configure the external API wrappe
            var wrapper = ExternalApiFactory.GetWrapperInstance(Logger, TrackerHttpClient.Instance, Context, null, _serviceType, ApiEndpointType.ActiveFlights, Settings);

            // Extract the lookup parameters from the command line
            var icao = Parser.GetValues(CommandLineOptionType.Weather)[0];

            // Perform the lookup
            var results = await wrapper.LookupCurrentAirportWeather(icao);
            if (results?.Count() > 0)
            {
                foreach (var result in results)
                {
                    Console.WriteLine($"Weather for {icao} : {result}");
                }
            }
            else
            {
                Console.WriteLine($"No weather results returned for {icao}");
            }
        }
    }
}
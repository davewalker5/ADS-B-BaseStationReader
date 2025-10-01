
using BaseStationReader.Entities.Config;
using BaseStationReader.Entities.Logging;
using BaseStationReader.Interfaces.Api;
using BaseStationReader.Interfaces.Logging;

namespace BaseStationReader.BusinessLogic.Api.Wrapper
{
    internal class AirportWeatherApiWrapper : IAirportWeatherApiWrapper
    {
        private readonly ITrackerLogger _logger;
        private readonly IExternalApiRegister _register;

        public AirportWeatherApiWrapper(
            ITrackerLogger logger,
            IExternalApiRegister register)
        {
            _logger = logger;
            _register = register;
        }

        /// <summary>
        /// Lookup the current weather for an airport
        /// </summary>
        /// <param name="icao"></param>
        /// <returns></returns>
        public async Task<IEnumerable<string>> LookupAirportWeather(string icao)
        {
            // Get the API instance
            if (_register.GetInstance(ApiEndpointType.METAR) is not IMetarApi api) return null;

            _logger.LogMessage(Severity.Info, $"Looking up weather for airport ICAO = '{icao}'");

            // Lookup the weather for the requested airport
            var results = await api.LookupAirportWeather(icao);

            // Log the results
            if (results?.Count() > 0)
            {
                foreach (var result in results)
                {
                    _logger.LogMessage(Severity.Info, $"Weather for {icao} : {result}");
                }
            }
            else
            {
                _logger.LogMessage(Severity.Warning, $"No weather results returned for {icao}");
            }

            return results;
        }
    }
}
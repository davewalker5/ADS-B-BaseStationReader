
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
        public async Task<IEnumerable<string>> LookupCurrentAirportWeather(string icao)
        {
            // Get the API instance
            if (_register.GetInstance(ApiEndpointType.METAR) is not IMetarApi api) return null;

            _logger.LogMessage(Severity.Info, $"Looking up weather for airport ICAO = '{icao}'");

            // Lookup the weather for the requested airport
            var results = await api.LookupCurrentAirportWeather(icao);

            // Log the results
            LogWeatherReports(icao, results);

            return results;
        }

        /// <summary>
        /// Lookup the weather forecast for an airport
        /// </summary>
        /// <param name="icao"></param>
        /// <returns></returns>
        public async Task<IEnumerable<string>> LookupAirportWeatherForecast(string icao)
        {
            // Get the API instance
            if (_register.GetInstance(ApiEndpointType.TAF) is not ITafApi api) return null;

            _logger.LogMessage(Severity.Info, $"Looking up weather for airport ICAO = '{icao}'");

            // Lookup the weather for the requested airport
            var results = await api.LookupAirportWeatherForecast(icao);

            // Log the results
            LogWeatherReports(icao, results);

            return results;
        }

        /// <summary>
        /// Log the weather reports returned by the API
        /// </summary>
        /// <param name="icao"></param>
        /// <param name="reports"></param>
        private void LogWeatherReports(string icao, IEnumerable<string> reports)
        {
            // Log the results
            if (reports?.Count() > 0)
            {
                foreach (var result in reports)
                {
                    _logger.LogMessage(Severity.Info, $"Weather for {icao} : {result}");
                }
            }
            else
            {
                _logger.LogMessage(Severity.Warning, $"No weather results returned for {icao}");
            }
        }
    }
}
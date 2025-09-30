using BaseStationReader.Entities.Config;
using BaseStationReader.Interfaces.Logging;
using BaseStationReader.Entities.Logging;
using BaseStationReader.Interfaces.Api;

namespace BaseStationReader.BusinessLogic.Api.CheckWXApi
{
    public class CheckWXMetarApi : ExternalApiBase, IMetarApi
    {
        private readonly string _baseAddress;
        private readonly string _key;

        public CheckWXMetarApi(
            ITrackerLogger logger,
            ITrackerHttpClient client,
            string url,
            string key) : base(logger, client)
        {
            _baseAddress = url;
            _key = key;
        }

        /// <summary>
        /// Return a collection of METAR strings for the specified airport
        /// </summary>
        /// <param name="icao"></param>
        /// <returns></returns>
        public async Task<IEnumerable<string>> LookupAirportWeather(string icao)
        {
            Logger.LogMessage(Severity.Info, $"Looking up weather for airport with ICAO code {icao}");
            var results = await MakeApiRequestAsync(icao);
            return results;
        }

        /// <summary>
        /// Make a request to the specified URL and return the response properties as a dictionary
        /// </summary>
        /// <param name="parameters"></param>
        /// <returns></returns>
        private async Task<IEnumerable<string>> MakeApiRequestAsync(string parameters)
        {
            IEnumerable<string> results = null;

            try
            {
                // Make a request for the data from the API
                var url = $"{_baseAddress}/{parameters}";
                var node = await GetAsync(Logger, ApiServiceType.CheckWXApi, url, new()
                {
                    { "X-API-Key", _key }
                });

                if (node != null)
                {
                    // Extract the response element, which is an array of strings, from the JSON DOM and
                    // convert to a list of strings
                    var apiResponse = node!["data"]!.AsArray();
                    results = apiResponse!.Select(x => x!.ToString());

                    // Log the properties dictionary
                    foreach (var metar in results)
                    {
                        Logger.LogMessage(Severity.Debug, $"METAR for {parameters} : {metar}");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogMessage(Severity.Error, ex.Message);
                Logger.LogException(ex);
                results = null;
            }

            return results;
        }
    }
}
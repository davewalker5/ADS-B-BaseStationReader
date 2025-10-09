using BaseStationReader.Entities.Config;
using BaseStationReader.Entities.Logging;
using BaseStationReader.Interfaces.Api;
using BaseStationReader.Interfaces.Database;
using BaseStationReader.Interfaces.Logging;

namespace BaseStationReader.BusinessLogic.Api.SkyLink
{
    internal class SkyLinkMetarApi : SkyLinkApiBase, IMetarApi
    {
        private const ApiServiceType ServiceType = ApiServiceType.SkyLink;
        private readonly string _baseAddress;
        private readonly string _host;
        private readonly string _key;

        public SkyLinkMetarApi(
            ITrackerLogger logger,
            ITrackerHttpClient client,
            IDatabaseManagementFactory factory,
            ExternalApiSettings settings) : base(logger, client, factory)
        {
            // Get the API configuration properties and store the key
            var definition = settings.ApiServices.FirstOrDefault(x => x.Service == ServiceType);
            _key = definition?.Key;

            // Get the endpoint URL, set up the base address for requests and extract the host name
            var url = settings.ApiEndpoints.FirstOrDefault(x => x.EndpointType == ApiEndpointType.METAR && x.Service == ServiceType)?.Url;
            _baseAddress = $"{url}";
            _host = new Uri(url).Host;

            // Set the rate limit for this service on the HTTP client
            client.SetRateLimits(ServiceType, definition?.RateLimit ?? 0);
        }

        /// <summary>
        /// Return a collection of METAR strings for the specified airport
        /// </summary>
        /// <param name="icao"></param>
        /// <returns></returns>
        public async Task<IEnumerable<string>> LookupCurrentAirportWeatherAsync(string icao)
        {
            Logger.LogMessage(Severity.Info, $"Looking up weather for airport with ICAO code {icao}");
            var result = await MakeApiRequestAsync(icao);
            IEnumerable<string> results = string.IsNullOrEmpty(result) ? null : [result];
            return results;
        }

        /// <summary>
        /// Make a request to the specified URL
        /// </summary>
        /// <param name="parameters"></param>
        /// <returns></returns>
        private async Task<string> MakeApiRequestAsync(string parameters)
        {
            string metar = null;

            try
            {
                // Make a request for the data from the API
                var url = $"{_baseAddress}/{parameters}";
                var node = await GetAsync(Logger, ServiceType, url, new Dictionary<string, string>()
                {
                    { "X-RapidAPI-Key", _key },
                    { "X-RapidAPI-Host", _host },
                });

                // Get the weather report object from the response
                var report = GetResponseAsObject(node);
                if (report == null)
                {
                    return null;
                }

                // Extract the report and log it
                metar = report?["raw"]?.GetValue<string>() ?? "";
                Logger.LogMessage(Severity.Debug, $"METAR for {parameters} : {metar}");
            }
            catch (Exception ex)
            {
                Logger.LogMessage(Severity.Error, ex.Message);
                Logger.LogException(ex);
            }

            return metar;
        }
    }
}
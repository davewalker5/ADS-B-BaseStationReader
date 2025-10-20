using BaseStationReader.Entities.Api;
using BaseStationReader.Entities.Config;
using BaseStationReader.Entities.Logging;
using BaseStationReader.Interfaces.Api;
using BaseStationReader.Interfaces.Database;
using System.Diagnostics.CodeAnalysis;

namespace BaseStationReader.BusinessLogic.Api.CheckWXApi
{
    internal class CheckWXMetarApi : ExternalApiBase, IMetarApi
    {
        private const ApiServiceType ServiceType = ApiServiceType.CheckWXApi;
        private readonly string _baseAddress;
        private readonly string _key;

        [ExcludeFromCodeCoverage]
        public CheckWXMetarApi(
            ITrackerHttpClient client,
            IDatabaseManagementFactory factory,
            ExternalApiSettings settings) : base(client, factory)
        {
            // Get the API configuration properties and capture the API key
            var definition = settings.ApiServices.FirstOrDefault(x => x.Service == ServiceType);
            _key = definition?.Key;

            // Get the endpoint URL and set up the base address for requests
            _baseAddress = settings.ApiEndpoints.FirstOrDefault(x => x.EndpointType == ApiEndpointType.METAR && x.Service == ServiceType)?.Url;

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
            IEnumerable<string> results = null;

            Factory.Logger.LogMessage(Severity.Info, $"Looking up weather for airport with ICAO code {icao}");

            // Make a request for the data from the API
            var url = $"{_baseAddress}/{icao}";
            await Factory.ApiLogManager.AddAsync(ServiceType, ApiEndpointType.METAR, url, ApiProperty.AirportICAO, icao);
            var node = await GetAsync(ServiceType, url, new()
            {
                { "X-API-Key", _key }
            });

            if (node == null)
            {
                return null;
            }

            // Extract the response element, which is an array of strings, from the JSON DOM and
            // convert to a list of strings
            var apiResponse = node["data"]?.AsArray();
            if (apiResponse == null)
            {
                return null;
            }

            // Convert the reports to a list of strings, removing empty entries
            results = apiResponse.Where(x => x != null).Select(x => x.ToString()).Where(x => !string.IsNullOrEmpty(x));

            // Log the reports
            foreach (var metar in results)
            {
                Factory.Logger.LogMessage(Severity.Debug, $"METAR for {icao} : {metar}");
            }

            return results;
        }
    }
}
using BaseStationReader.Entities.Config;
using BaseStationReader.Interfaces.Logging;
using BaseStationReader.Entities.Logging;
using BaseStationReader.Entities.Api;
using BaseStationReader.Interfaces.Api;
using DocumentFormat.OpenXml.CustomProperties;
using System.Text.Json.Nodes;

namespace BaseStationReader.BusinessLogic.Api.AirLabs
{
    internal class AirLabsAirlinesApi : ExternalApiBase, IAirlinesApi
    {
        private const ApiServiceType ServiceType = ApiServiceType.AirLabs;
        private readonly string _baseAddress;

        public AirLabsAirlinesApi(
            ITrackerLogger logger,
            ITrackerHttpClient client,
            ExternalApiSettings settings) : base(logger, client)
        {
            // Get the API configuration properties
            var definition = settings.ApiServices.FirstOrDefault(x => x.Service == ServiceType);

            // Get the endpoint URL and set up the base address for requests
            var url = settings.ApiEndpoints.FirstOrDefault(x => x.EndpointType == ApiEndpointType.Aircraft && x.Service == ServiceType)?.Url;
            _baseAddress = $"{url}?api_key={definition?.Key}";

            // Set the rate limit for this service on the HTTP client
            client.SetRateLimits(ServiceType, definition?.RateLimit ?? 0);
        }

        /// <summary>
        /// Lookup an airline using its IATA code
        /// </summary>
        /// <param name="iata"></param>
        /// <returns></returns>
        public async Task<Dictionary<ApiProperty, string>> LookupAirlineByIATACodeAsync(string iata)
        {
            Logger.LogMessage(Severity.Info, $"Looking up airline with IATA code {iata}");
            return await MakeApiRequestAsync($"&iata_code={iata}");
        }

        /// <summary>
        /// Lookup an airline using it's ICAO code
        /// </summary>
        /// <param name="icao"></param>
        /// <returns></returns>
        public async Task<Dictionary<ApiProperty, string>> LookupAirlineByICAOCodeAsync(string icao)
        {
            Logger.LogMessage(Severity.Info, $"Looking up airline with ICAO code {icao}");
            return await MakeApiRequestAsync($"&icao_code={icao}");
        }

        /// <summary>
        /// Make a request to the specified URL and return the response properties as a dictionary
        /// </summary>
        /// <param name="parameters"></param>
        /// <returns></returns>
        private async Task<Dictionary<ApiProperty, string>> MakeApiRequestAsync(string parameters)
        {
            Dictionary<ApiProperty, string> properties = null;

            try
            {
                // Make a request for the data from the API
                var url = $"{_baseAddress}{parameters}";
                var node = await GetAsync(Logger, ApiServiceType.AirLabs, url, []);

                // Check we have a node
                if (node == null)
                {
                    Logger.LogMessage(Severity.Warning, $"API request returned a NULL response");
                    return properties;
                }

                // Extract the response element from the JSON DOM as a JSON array
                var response = node?["response"] as JsonArray;
                if (response?.Count == 0)
                {
                    Logger.LogMessage(Severity.Warning, "API request returned an empty response");
                    return properties;
                }

                // Extract the first element of the response as a JSON object
                if (response[0] is not JsonObject airline)
                {
                    Logger.LogMessage(Severity.Warning, "Unexpected API response format");
                    return properties;
                }

                // Extract the values into a dictionary
                properties = new()
                {
                    { ApiProperty.AirlineIATA, airline?["iata_code"]?.GetValue<string>() ?? "" },
                    { ApiProperty.AirlineICAO, airline?["icao_code"]?.GetValue<string>() ?? "" },
                    { ApiProperty.AirlineName, airline?["name"]?.GetValue<string>() ?? "" },
                };

                // Log the properties dictionary
                LogProperties("Airline", properties);
            }
            catch (Exception ex)
            {
                Logger.LogMessage(Severity.Error, ex.Message);
                Logger.LogException(ex);
                properties = null;
            }

            return HaveValidProperties(properties) ? properties : null;
        }

        /// <summary>
        /// Return true if we have sufficient properties to constitute a valid response
        /// </summary>
        /// <param name="properties"></param>
        /// <returns></returns>
        private static bool HaveValidProperties(Dictionary<ApiProperty, string> properties)
            => HaveValue(properties, ApiProperty.AirlineName);
    }
}

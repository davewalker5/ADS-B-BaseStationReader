using System.Diagnostics.CodeAnalysis;
using BaseStationReader.Entities.Api;
using BaseStationReader.Entities.Config;
using BaseStationReader.Entities.Logging;
using BaseStationReader.Interfaces.Api;
using BaseStationReader.Interfaces.Database;

namespace BaseStationReader.BusinessLogic.Api.SkyLink
{
    internal class SkyLinkAirlinesApi : SkyLinkApiBase, IAirlinesApi
    {
        private const ApiServiceType ServiceType = ApiServiceType.SkyLink;
        private readonly string _baseAddress;
        private readonly string _host;
        private readonly string _key;

        [ExcludeFromCodeCoverage]
        public SkyLinkAirlinesApi(
            ITrackerHttpClient client,
            IDatabaseManagementFactory factory,
            ExternalApiSettings settings) : base(client, factory)
        {
            // Get the API configuration properties and store the key
            var definition = settings.ApiServices.FirstOrDefault(x => x.Service == ServiceType);
            _key = definition?.Key;

            // Get the endpoint URL, set up the base address for requests and extract the host name
            var url = settings.ApiEndpoints.FirstOrDefault(x => x.EndpointType == ApiEndpointType.Airlines && x.Service == ServiceType)?.Url;
            _baseAddress = $"{url}";
            _host = new Uri(url).Host;

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
            Factory.Logger.LogMessage(Severity.Info, $"Looking up airline with IATA code {iata}");
            return await MakeApiRequestAsync(ApiProperty.AirlineIATA, $"?iata={iata}");
        }

        /// <summary>
        /// Lookup an airline using it's ICAO code
        /// </summary>
        /// <param name="icao"></param>
        /// <returns></returns>
        public async Task<Dictionary<ApiProperty, string>> LookupAirlineByICAOCodeAsync(string icao)
        {
            Factory.Logger.LogMessage(Severity.Info, $"Looking up airline with ICAO code {icao}");
            return await MakeApiRequestAsync(ApiProperty.AirlineICAO, $"?icao={icao}");
        }

        /// <summary>
        /// Make a request to the specified URL
        /// </summary>
        /// <param name="property"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        private async Task<Dictionary<ApiProperty, string>> MakeApiRequestAsync(ApiProperty property, string parameters)
        {
            Dictionary<ApiProperty, string> properties = [];

            // Make a request for the data from the API
            var url = $"{_baseAddress}{parameters}";
            await Factory.ApiLogManager.AddAsync(ServiceType, ApiEndpointType.Airlines, url, property, parameters);
            var node = await GetAsync(ServiceType, url, new Dictionary<string, string>()
            {
                { "X-RapidAPI-Key", _key },
                { "X-RapidAPI-Host", _host },
            });

            // Get the airline object from the response
            var airline = GetFirstResponseObject(node);
            if (airline == null)
            {
                return null;
            }

            // Extract the values into a dictionary
            properties = new()
            {
                { ApiProperty.AirlineIATA, GetStringValue(airline, "iata") },
                { ApiProperty.AirlineICAO, GetStringValue(airline, "icao") },
                { ApiProperty.AirlineName, GetStringValue(airline, "name") }
            };

            // Log the properties dictionary
            LogProperties("Airline", properties);

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
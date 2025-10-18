using BaseStationReader.Entities.Config;
using BaseStationReader.Entities.Logging;
using BaseStationReader.Entities.Api;
using BaseStationReader.Interfaces.Api;
using System.Diagnostics.CodeAnalysis;
using BaseStationReader.Interfaces.Database;

namespace BaseStationReader.BusinessLogic.Api.AirLabs
{
    internal class AirLabsAirlinesApi : AirLabsApiBase, IAirlinesApi
    {
        private const ApiServiceType ServiceType = ApiServiceType.AirLabs;
        private readonly string _baseAddress;

        [ExcludeFromCodeCoverage]
        public AirLabsAirlinesApi(
            ITrackerHttpClient client,
            IDatabaseManagementFactory factory,
            ExternalApiSettings settings) : base(client, factory)
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
            Factory.Logger.LogMessage(Severity.Info, $"Looking up airline with IATA code {iata}");
            return await MakeApiRequestAsync($"&iata_code={iata}");
        }

        /// <summary>
        /// Lookup an airline using it's ICAO code
        /// </summary>
        /// <param name="icao"></param>
        /// <returns></returns>
        public async Task<Dictionary<ApiProperty, string>> LookupAirlineByICAOCodeAsync(string icao)
        {
            Factory.Logger.LogMessage(Severity.Info, $"Looking up airline with ICAO code {icao}");
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

            // Make a request for the data from the API
            var url = $"{_baseAddress}{parameters}";
            var node = await GetAsync(ServiceType, url, []);

            // Get the aircraft object from the response
            var airline = GetFirstResponseObject(node);
            if (airline == null)
            {
                return null;
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

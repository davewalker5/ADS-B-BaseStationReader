using System.Globalization;
using System.Text.Json.Nodes;
using BaseStationReader.Entities.Config;
using BaseStationReader.Entities.Logging;
using BaseStationReader.Entities.Api;
using BaseStationReader.Interfaces.Api;
using System.Diagnostics.CodeAnalysis;
using BaseStationReader.Interfaces.Database;

namespace BaseStationReader.BusinessLogic.Api.AeroDatabox
{
    internal class AeroDataBoxAircraftApi : ExternalApiBase, IAircraftApi
    {
        private const ApiServiceType ServiceType = ApiServiceType.AeroDataBox;
        private readonly string _baseAddress;
        private readonly string _host;
        private readonly string _key;

        [ExcludeFromCodeCoverage]
        public AeroDataBoxAircraftApi(
            ITrackerHttpClient client,
            IDatabaseManagementFactory factory,
            ExternalApiSettings settings) : base(client, factory)
        {
            // Get the API configuration properties and store the key
            var definition = settings.ApiServices.FirstOrDefault(x => x.Service == ServiceType);
            _key = definition?.Key;

            // Get the endpoint URL, set up the base address for requests and extract the host name
            var url = settings.ApiEndpoints.FirstOrDefault(x => x.EndpointType == ApiEndpointType.Aircraft && x.Service == ServiceType)?.Url;
            _baseAddress = $"{url}/icao24/";
            _host = new Uri(url).Host;

            // Set the rate limit for this service on the HTTP client
            client.SetRateLimits(ServiceType, definition?.RateLimit ?? 0);
        }

        /// <summary>
        /// Lookup an aircraft's details using its ICAO 24-bit address
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public async Task<Dictionary<ApiProperty, string>> LookupAircraftAsync(string address)
        {
            Factory.Logger.LogMessage(Severity.Info, $"Looking up aircraft with address {address}");
            var properties = await MakeApiRequestAsync($"{address}");
            return properties;
        }

        /// <summary>
        /// Make a request to the specified URL
        /// </summary>
        /// <param name="parameters"></param>
        /// <returns></returns>
        private async Task<Dictionary<ApiProperty, string>> MakeApiRequestAsync(string parameters)
        {
            Dictionary<ApiProperty, string> properties = [];

            // Make a request for the data from the API
            var url = $"{_baseAddress}{parameters}";
            var node = await GetAsync(ServiceType, url, new Dictionary<string, string>()
            {
                { "X-RapidAPI-Key", _key },
                { "X-RapidAPI-Host", _host },
            });

            if (node != null)
            {
                // Extract the delivery date and use it to determine year of manufacture and age
                int? manufactured = GetYearOfManufacture(node as JsonObject);
                int? age = manufactured != null ? DateTime.Today.Year - manufactured : null;

                // Extract the values into a dictionary
                properties = new()
                {
                    { ApiProperty.AircraftRegistration, GetStringValue(node, "reg")},
                    { ApiProperty.AircraftManufactured, manufactured?.ToString() ?? "" },
                    { ApiProperty.AircraftAge, age?.ToString() ?? "" },
                    { ApiProperty.ModelICAO, GetStringValue(node, "icaoCode")},
                    { ApiProperty.ModelIATA, GetStringValue(node, "iataCodeShort")},
                    { ApiProperty.ModelName, GetStringValue(node, "typeName")},
                    { ApiProperty.ManufacturerName, "" }
                };

                // Log the properties dictionary
                LogProperties("Aircraft", properties);
            }

            return HaveValidProperties(properties) ? properties : null;
        }

        /// <summary>
        /// Return true if we have sufficient properties to constitute a valid response
        /// </summary>
        /// <param name="properties"></param>
        /// <returns></returns>
        private static bool HaveValidProperties(Dictionary<ApiProperty, string> properties)
            => HaveValue(properties, ApiProperty.AircraftRegistration);

        /// <summary>
        /// Extract the year of manufacture from the response
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        private static int? GetYearOfManufacture(JsonObject node)
        {
            int? year = null;

            // Extract the delivery date from the response and attempt to parse it as a date
            var deliveryDate = GetStringValue(node, "deliveryDate");
            if (!string.IsNullOrEmpty(deliveryDate) &&
                DateTime.TryParseExact(deliveryDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime delivered))
            {
                year = delivered.Year;
            }

            return year;
        }
    }
}
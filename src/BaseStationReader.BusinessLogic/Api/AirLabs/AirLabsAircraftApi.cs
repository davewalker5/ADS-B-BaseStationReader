using BaseStationReader.Entities.Config;
using BaseStationReader.Interfaces.Logging;
using BaseStationReader.Entities.Logging;
using BaseStationReader.Entities.Api;
using BaseStationReader.Interfaces.Api;
using BaseStationReader.Interfaces.Database;

namespace BaseStationReader.BusinessLogic.Api.AirLabs
{
    internal class AirLabsAircraftApi : AirLabsApiBase, IAircraftApi
    {
        private const ApiServiceType ServiceType = ApiServiceType.AirLabs;
        private readonly string _baseAddress;

        public AirLabsAircraftApi(
            ITrackerLogger logger,
            ITrackerHttpClient client,
            IDatabaseManagementFactory factory,
            ExternalApiSettings settings) : base(logger, client, factory)
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
        /// Lookup an aircraft's details using its ICAO 24-bit address
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public async Task<Dictionary<ApiProperty, string>> LookupAircraftAsync(string address)
        {
            Logger.LogMessage(Severity.Info, $"Looking up aircraft with address {address}");
            var properties = await MakeApiRequestAsync($"&hex={address}");
            return properties;
        }

        /// <summary>
        /// Make a request to the specified URL
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
                var node = await GetAsync(Logger, ServiceType, url, []);

                // Get the aircraft object from the response
                var aircraft = GetFirstResponseObject(node);
                if (aircraft == null)
                {
                    return null;
                }

                // Extract the year the aircraft was built and use it to determine the age
                int? manufactured = aircraft?["built"]?.GetValue<int?>();
                int? age = manufactured != null ? DateTime.Today.Year - manufactured : null;

                // Extract the values into a dictionary
                properties = new()
                {
                    { ApiProperty.AircraftRegistration, aircraft?["reg_number"]?.GetValue<string>() ?? "" },
                    { ApiProperty.AircraftManufactured, manufactured?.ToString() ?? "" },
                    { ApiProperty.AircraftAge, age?.ToString() ?? "" },
                    { ApiProperty.ManufacturerName, aircraft?["manufacturer"]?.GetValue<string>() ?? "" },
                    { ApiProperty.ModelICAO, aircraft?["icao"]?.GetValue<string>() ?? "" },
                    { ApiProperty.ModelIATA, aircraft?["iata"]?.GetValue<string>() ?? "" },
                    { ApiProperty.ModelName, aircraft?["model"]?.GetValue<string>() ?? "" }
                };

                // Log the properties dictionary
                LogProperties("Aircraft", properties);
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
            => HaveValue(properties, ApiProperty.AircraftRegistration);
    }
}

using System.Text.Json.Nodes;
using BaseStationReader.Entities.Api;
using BaseStationReader.Entities.Config;
using BaseStationReader.Entities.Logging;
using BaseStationReader.Interfaces.Api;
using BaseStationReader.Interfaces.Database;
using BaseStationReader.Interfaces.Logging;

namespace BaseStationReader.BusinessLogic.Api.SkyLink
{
    internal class SkyLinkAircraftApi : SkyLinkApiBase, IAircraftApi
    {
        private const ApiServiceType ServiceType = ApiServiceType.SkyLink;
        private readonly string _baseAddress;
        private readonly string _host;
        private readonly string _key;

        public SkyLinkAircraftApi(
            ITrackerLogger logger,
            ITrackerHttpClient client,
            IDatabaseManagementFactory factory,
            ExternalApiSettings settings) : base(logger, client, factory)
        {
            // Get the API configuration properties and store the key
            var definition = settings.ApiServices.FirstOrDefault(x => x.Service == ServiceType);
            _key = definition?.Key;

            // Get the endpoint URL, set up the base address for requests and extract the host name
            var url = settings.ApiEndpoints.FirstOrDefault(x => x.EndpointType == ApiEndpointType.Aircraft && x.Service == ServiceType)?.Url;
            _baseAddress = $"{url}";
            _host = new Uri(url).Host;

            // Set the rate limit for this service on the HTTP client
            client.SetRateLimits(ServiceType, definition?.RateLimit ?? 0);
        }

        /// <summary>
        /// Lookup an aircraft given it's 24-bit ICAO address
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public async Task<Dictionary<ApiProperty, string>> LookupAircraftAsync(string address)
        {
            Logger.LogMessage(Severity.Info, $"Looking up aircraft with address {address}");
            var properties = await MakeApiRequestAsync($"?icao={address}");
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

            try
            {
                // Make a request for the data from the API
                var url = $"{_baseAddress}{parameters}";
                var node = await GetAsync(Logger, ServiceType, url, new Dictionary<string, string>()
                {
                    { "X-RapidAPI-Key", _key },
                    { "X-RapidAPI-Host", _host },
                });

                // Extract the response as a JSON object
                var response = GetResponseAsObject(node);
                if (GetResponseAsObject == null)
                {
                    return null;
                }

                // Get the aircraft list from the object
                var aircraftList = response["aircraft"] as JsonArray;
                if (aircraftList == null)
                {
                    Logger.LogMessage(Severity.Warning, $"Aircraft list from the response is NULL");
                    return null;
                }

                // Check there are some aircraft in the list
                if (aircraftList.Count == 0)
                {
                    Logger.LogMessage(Severity.Warning, $"Aircraft list from the response is empty");
                    return null;
                }

                // Extract the aircraft
                var aircraft = aircraftList.First() as JsonObject;
                if (aircraft == null)
                {
                    Logger.LogMessage(Severity.Warning, $"First element in the aircraft list is not a JSON object");
                    return null;
                }

                // Extract the values into a dictionary
                properties = new()
                {
                    { ApiProperty.AircraftRegistration, aircraft?["registration"]?.GetValue<string>() ?? "" },
                    { ApiProperty.ModelICAO, aircraft?["aircraft_type"]?.GetValue<string>() ?? "" },
                    { ApiProperty.Callsign, aircraft?["callsign"]?.GetValue<string>() ?? "" },
                };

                // Log the properties dictionary
                LogProperties("Aircraft", properties);
            }
            catch (Exception ex)
            {
                Logger.LogMessage(Severity.Error, ex.Message);
                Logger.LogException(ex);
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
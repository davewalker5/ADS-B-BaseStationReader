using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;
using BaseStationReader.Entities.Api;
using BaseStationReader.Entities.Config;
using BaseStationReader.Entities.Logging;
using BaseStationReader.Interfaces.Api;
using BaseStationReader.Interfaces.Database;

namespace BaseStationReader.Api.SkyLink
{
    internal class SkyLinkAircraftApi : SkyLinkApiBase, IAircraftApi
    {
        private const ApiServiceType ServiceType = ApiServiceType.SkyLink;
        private readonly string _baseAddress;
        private readonly string _host;
        private readonly string _key;

        [ExcludeFromCodeCoverage]
        public SkyLinkAircraftApi(
            ITrackerHttpClient client,
            IDatabaseManagementFactory factory,
            ExternalApiSettings settings) : base(client, factory)
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
            Dictionary<ApiProperty, string> properties = [];

            Factory.Logger.LogMessage(Severity.Info, $"Looking up aircraft with address {address}");

            // Make a request for the data from the API
            var url = $"{_baseAddress}?icao24={address}";
            await Factory.ApiLogManager.AddAsync(ServiceType, ApiEndpointType.Aircraft, url, ApiProperty.AircraftAddress, address);
            var node = await GetAsync(ServiceType, url, new Dictionary<string, string>()
            {
                { "X-RapidAPI-Key", _key },
                { "X-RapidAPI-Host", _host },
            });

            // Extract the response as a JSON object
            var response = GetResponseAsObject(node);
            if (response == null)
            {
                return null;
            }

            // Get the aircraft list from the object
            var aircraftList = response["aircraft"] as JsonArray;
            if (aircraftList == null)
            {
                Factory.Logger.LogMessage(Severity.Warning, $"Aircraft list from the response is NULL");
                return null;
            }

            // Check there are some aircraft in the list
            if (aircraftList.Count == 0)
            {
                Factory.Logger.LogMessage(Severity.Warning, $"Aircraft list from the response is empty");
                return null;
            }

            // Extract the aircraft
            var aircraft = aircraftList.First() as JsonObject;
            if (aircraft == null)
            {
                Factory.Logger.LogMessage(Severity.Warning, $"First element in the aircraft list is not a JSON object");
                return null;
            }

            // Extract the values into a dictionary
            properties = new()
            {
                { ApiProperty.AircraftRegistration, GetStringValue(aircraft, "registration") },
                { ApiProperty.ModelICAO, GetStringValue(aircraft, "aircraft_type") },
                { ApiProperty.ModelIATA, "" },
                { ApiProperty.ModelName, "" },
                { ApiProperty.Callsign, GetStringValue(aircraft, "callsign") },
                { ApiProperty.AircraftManufactured, "" },
                { ApiProperty.ManufacturerName, "" }
            };

            // Log the properties dictionary
            LogProperties("Aircraft", properties);

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
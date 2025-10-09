using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using BaseStationReader.Entities.Api;
using BaseStationReader.Entities.Config;
using BaseStationReader.Entities.Logging;
using BaseStationReader.Interfaces.Api;
using BaseStationReader.Interfaces.Database;
using BaseStationReader.Interfaces.Logging;

namespace BaseStationReader.BusinessLogic.Api.SkyLink
{
    internal class SkyLinkFlightApiBase : SkyLinkApiBase
    {
        private const ApiServiceType ServiceType = ApiServiceType.SkyLink;

        private readonly List<ApiProperty> _supportedProperties = [
            ApiProperty.FlightNumber
        ];

        private readonly string _baseAddress;
        private readonly string _host;
        private readonly string _key;

        public SkyLinkFlightApiBase(
            ApiEndpointType type,
            ITrackerLogger logger,
            ITrackerHttpClient client,
            IDatabaseManagementFactory factory,
            ExternalApiSettings settings) : base(logger, client, factory)
        {
            // Get the API configuration properties and store the key
            var definition = settings.ApiServices.FirstOrDefault(x => x.Service == ServiceType);
            _key = definition?.Key;

            // Get the endpoint URL, set up the base address for requests and extract the host name
            var url = settings.ApiEndpoints.FirstOrDefault(x => x.EndpointType == type && x.Service == ServiceType)?.Url;
            _baseAddress = $"{url}";
            _host = new Uri(url).Host;

            // Set the rate limit for this service on the HTTP client
            client.SetRateLimits(ServiceType, definition?.RateLimit ?? 0);
        }

        /// <summary>
        /// Return true if this implementation supports flight lookup by the specified property
        /// </summary>
        /// <param name="propertyType"></param>
        /// <returns></returns>
        public bool SupportsLookupBy(ApiProperty propertyType)
            => _supportedProperties.Contains(propertyType);

        /// <summary>
        /// Look up a flight given the flight number
        /// </summary>
        /// <param name="flightNumber"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public async Task<Dictionary<ApiProperty, string>> LookupFlightByNumberAsync(string flightNumber)
        {
            Logger.LogMessage(Severity.Info, $"Looking up active flight using flight number {flightNumber}");
            var properties = await MakeApiRequestAsync($"/{flightNumber}");
            return properties?.Count > 0 ? properties : null;
        }

        /// <summary>
        /// Make a request to the specified URL
        /// </summary>
        /// <param name="parameters"></param>
        /// <returns></returns>
        protected async Task<Dictionary<ApiProperty, string>> MakeApiRequestAsync(string parameters)
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

                // Get the flight object from the response
                var flight = GetResponseAsObject(node);
                if (flight == null)
                {
                    return null;
                }

                // Extract the flight number and split out the airline IATA and the numeric part
                var airlineIATA = "";
                var flightNumber = "";
                var flightIATA = flight?["flight_number"]?.GetValue<string>() ?? "";
                var match = Regex.Match(flightIATA, @"^([A-Za-z]+)(\d+)$");
                if (match.Success)
                {
                    airlineIATA = match.Groups[1].Value;
                    flightNumber = match.Groups[2].Value;
                }

                // Extract the values into a dictionary
                properties = new()
                {
                    { ApiProperty.FlightNumber, flightNumber },
                    { ApiProperty.FlightICAO, "" },
                    { ApiProperty.FlightIATA, flightIATA },
                    { ApiProperty.AirlineIATA, airlineIATA},
                    { ApiProperty.AirlineICAO, ""}
                };

                // Extract the airport details
                ExtractEmbarkationAirport(flight, properties);
                ExtractDestinationAirport(flight, properties);

                // Log the properties dictionary
                LogProperties("Flight", properties);
            }
            catch (Exception ex)
            {
                Logger.LogMessage(Severity.Error, ex.Message);
                Logger.LogException(ex);
            }

            return HaveValidProperties(properties) ? properties : null;
        }

        /// <summary>
        /// Extract the properties of the embarkation airport
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        private void ExtractEmbarkationAirport(JsonNode node, Dictionary<ApiProperty, string> properties)
        {
            // Find the departure airport node
            var airport = node?["departure"];

            Logger.LogMessage(Severity.Debug, $"Extracting embarkation airport details from {airport?.ToJsonString()}");

            // Extract the airport property - this should be a string with the IATA code followed by a separator then the
            // name
            var airportDetails = airport?["airport"]?.GetValue<string>() ?? "";
            var iata = !string.IsNullOrEmpty(airportDetails) ? airportDetails[..3] : "";

            // Extract the properties of interest from the node
            properties.Add(ApiProperty.EmbarkationIATA, iata);
        }

        /// <summary>
        /// Extract the properties of the embarkation airport
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        private void ExtractDestinationAirport(JsonNode node, Dictionary<ApiProperty, string> properties)
        {
            // Find the departure airport node
            var airport = node?["arrival"];

            Logger.LogMessage(Severity.Debug, $"Extracting destination airport details from {airport?.ToJsonString()}");

            // Extract the airport property - this should be a string with the IATA code followed by a separator then the
            // name
            var airportDetails = airport?["airport"]?.GetValue<string>() ?? "";
            var iata = !string.IsNullOrEmpty(airportDetails) ? airportDetails[..3] : "";

            // Extract the properties of interest from the node
            properties.Add(ApiProperty.DestinationIATA, iata);
        }

        /// <summary>
        /// Return true if we have sufficient properties to constitute a valid response
        /// </summary>
        /// <param name="properties"></param>
        /// <returns></returns>
        private static bool HaveValidProperties(Dictionary<ApiProperty, string> properties)
            => HaveValue(properties, ApiProperty.FlightIATA) && HaveValue(properties, ApiProperty.AirlineIATA);
    }
}
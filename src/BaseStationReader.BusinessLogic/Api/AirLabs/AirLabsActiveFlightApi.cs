using System.Text.Json.Nodes;
using BaseStationReader.Entities.Config;
using BaseStationReader.Entities.Logging;
using BaseStationReader.Entities.Api;
using BaseStationReader.Interfaces.Api;
using System.Diagnostics.CodeAnalysis;
using BaseStationReader.Interfaces.Database;

namespace BaseStationReader.BusinessLogic.Api.AirLabs
{
    internal class AirLabsActiveFlightApi : AirLabsApiBase, IActiveFlightsApi
    {
        private const ApiServiceType ServiceType = ApiServiceType.AirLabs;

        private readonly List<ApiProperty> _supportedProperties = [
            ApiProperty.AircraftAddress
        ];

        private readonly string _baseAddress;

        [ExcludeFromCodeCoverage]
        public AirLabsActiveFlightApi(
            ITrackerHttpClient client,
            IDatabaseManagementFactory factory,
            ExternalApiSettings settings) : base(client, factory)
        {
            // Get the API configuration properties
            var definition = settings.ApiServices.FirstOrDefault(x => x.Service == ServiceType);

            // Get the endpoint URL and set up the base address for requests
            var url = settings.ApiEndpoints.FirstOrDefault(x => x.EndpointType == ApiEndpointType.ActiveFlights && x.Service == ServiceType)?.Url;
            _baseAddress = $"{url}?api_key={definition?.Key}";

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
        /// Lookup an active flight's details using the aircraft's ICAO 24-bit address
        /// </summary>
        /// <param name="propertyType"></param>
        /// <param name="propertyValue"></param>
        /// <returns></returns>
        public async Task<Dictionary<ApiProperty, string>> LookupFlightAsync(ApiProperty propertyType, string propertyValue)
        {
            Factory.Logger.LogMessage(Severity.Info, $"Looking up active flight using {propertyType} {propertyValue}");
            var properties = await MakeApiRequestAsync($"&hex={propertyValue}");
            return properties.Count > 0 ? properties.First() : null;
        }

        /// <summary>
        /// Make a request to the specified URL
        /// </summary>
        /// <param name="parameters"></param>
        /// <returns></returns>
        private async Task<List<Dictionary<ApiProperty, string>>> MakeApiRequestAsync(string parameters)
        {
            List<Dictionary<ApiProperty, string>> properties = [];

            // Make a request for the data from the API
            var url = $"{_baseAddress}{parameters}";
            var node = await GetAsync(ServiceType, url, []);

            // Get the response array
            var flightList = GetResponseAsObjectList(node);
            if (flightList == null)
            {
                return properties;
            }

            // Iterate over each (presumed) flight in the response
            foreach (var flight in flightList)
            {
                // Extract the flight properties into a dictionary
                var flightProperties = ExtractSingleFlight(flight);

                // Log the properties dictionary
                LogProperties("Flight", flightProperties);
            
                // Add the flight properties to the collection
                properties.Add(flightProperties);
            }

            return properties;
        }

        /// <summary>
        /// Extract properties for a single flight into a dictionary
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        internal static Dictionary<ApiProperty, string> ExtractSingleFlight(JsonObject node)
        {
            // Extract the airline IATA code and flight IATA code from the response
            var airlineIATA = GetStringValue(node, "airline_iata");
            var flightIATA = ExtractFlightIATA(node, airlineIATA);

            // Extract the properties of interest from the node
            Dictionary<ApiProperty, string> properties = new()
            {
                { ApiProperty.EmbarkationIATA, GetStringValue(node, "dep_iata")},
                { ApiProperty.DestinationIATA, GetStringValue(node, "arr_iata")},
                { ApiProperty.FlightIATA, flightIATA },
                { ApiProperty.FlightICAO, GetStringValue(node, "flight_icao")},
                { ApiProperty.AirlineIATA, airlineIATA },
                { ApiProperty.AirlineICAO, GetStringValue(node, "airline_icao")},
                { ApiProperty.AirlineName, "" },
                { ApiProperty.ModelICAO, GetStringValue(node, "aircraft_icao")},
                { ApiProperty.AircraftAddress, GetStringValue(node, "hex")}
            };

            return properties;
        }

        /// <summary>
        /// Extract the flight IATA code from the response
        /// </summary>
        /// <param name="node"></param>
        /// <param name="airlineIATA"></param>
        /// <returns></returns>
        internal static string ExtractFlightIATA(JsonNode node, string airlineIATA)
        {
            // Extract the flight IATA code member of the response. If that returns a value, trust it
            // as the flight IATA code
            var iata = GetStringValue(node, "flight_iata");
            if (string.IsNullOrEmpty(iata) && !string.IsNullOrEmpty(airlineIATA))
            {
                // No flight IATA in the response but we have a valid airline IATA code. Use that plus
                // the numeric flight number to construct the flight IATA
                var flightNumber = GetStringValue(node, "flight_number");
                if (!string.IsNullOrEmpty(flightNumber))
                {
                    iata = $"{airlineIATA}{flightNumber}";
                }
            }

            return iata;
        }
    }
}

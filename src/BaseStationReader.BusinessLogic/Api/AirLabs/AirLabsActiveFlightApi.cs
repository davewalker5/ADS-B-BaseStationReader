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
                // Extract the flight properties into a dictionary and add them to the collection
                // of flight property dictionaries
                var flightProperties = ExtractSingleFlight(flight);
                properties.Add(flightProperties);
            }

            return properties;
        }

        /// <summary>
        /// Extract properties for a single flight into a dictionary
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        private Dictionary<ApiProperty, string> ExtractSingleFlight(JsonObject node)
        {
            // Extract the airline IATA code and flight IATA code from the response
            var airlineIATA = node?["airline_iata"]?.GetValue<string>() ?? "";
            var flightIATA = ExtractFlightIATA(node, airlineIATA);

            // Extract the properties of interest from the node
            Dictionary<ApiProperty, string> properties = new()
            {
                { ApiProperty.EmbarkationIATA, node?["dep_iata"]?.GetValue<string>() ?? "" },
                { ApiProperty.DestinationIATA, node?["arr_iata"]?.GetValue<string>() ?? "" },
                { ApiProperty.FlightIATA, flightIATA },
                { ApiProperty.FlightICAO, node?["flight_icao"]?.GetValue<string>() ?? "" },
                { ApiProperty.AirlineIATA, airlineIATA },
                { ApiProperty.AirlineICAO, node?["airline_icao"]?.GetValue<string>() ?? "" },
                { ApiProperty.AirlineName, "" },
                { ApiProperty.ModelICAO, node?["aircraft_icao"]?.GetValue<string>() ?? "" },
                { ApiProperty.AircraftAddress, node?["hex"]?.GetValue<string>() ?? "" },
            };

            // Log the properties dictionary
            LogProperties("Flight", properties);

            return properties;
        }

        /// <summary>
        /// Extract the flight IATA code from the response
        /// </summary>
        /// <param name="node"></param>
        /// <param name="airlineIATA"></param>
        /// <returns></returns>
        private string ExtractFlightIATA(JsonNode node, string airlineIATA)
        {
            // Extract the flight IATA code member of the response. If that returns a value, trust it
            // as the flight IATA code
            var iata = node?["flight_iata"]?.GetValue<string>() ?? "";
            if (string.IsNullOrEmpty(iata) && !string.IsNullOrEmpty(airlineIATA))
            {
                // No flight IATA in the response but we have a valid airline IATA code. Use that plus
                // the numeric flight number to construct the flight IATA
                var flightNumber = node?["flight_number"]?.GetValue<string>() ?? "";
                if (!string.IsNullOrEmpty(flightNumber))
                {
                    iata = $"{airlineIATA}{flightNumber}";
                }
            }

            return iata;
        }
    }
}

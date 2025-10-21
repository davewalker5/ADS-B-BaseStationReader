using System.Text.Json.Nodes;
using BaseStationReader.Entities.Config;
using BaseStationReader.Entities.Logging;
using BaseStationReader.Entities.Api;
using BaseStationReader.Interfaces.Api;
using System.Diagnostics.CodeAnalysis;
using BaseStationReader.Interfaces.Database;

namespace BaseStationReader.Api.AirLabs
{
    internal class AeroDataBoxHistoricalFlightApi : ExternalApiBase, IHistoricalFlightsApi
    {
        private const ApiServiceType ServiceType = ApiServiceType.AeroDataBox;
        private readonly string _baseAddress;
        private readonly string _host;
        private readonly string _key;

        private readonly List<ApiProperty> _supportedProperties = [
            ApiProperty.AircraftAddress
        ];

        [ExcludeFromCodeCoverage]
        public AeroDataBoxHistoricalFlightApi(
            ITrackerHttpClient client,
            IDatabaseManagementFactory factory,
            ExternalApiSettings settings) : base(client, factory)
        {
            // Get the API configuration properties and store the key
            var definition = settings.ApiServices.FirstOrDefault(x => x.Service == ServiceType);
            _key = definition?.Key;

            // Get the endpoint URL, set up the base address for requests and extract the host name
            var url = settings.ApiEndpoints.FirstOrDefault(x => x.EndpointType == ApiEndpointType.HistoricalFlights && x.Service == ServiceType)?.Url;
            _baseAddress = $"{url}/icao24/";
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
        /// Lookup flight details using a date and time
        /// </summary>
        /// <param name="address"></param>
        /// <param name="date"></param>
        /// <returns></returns>
        public async Task<List<Dictionary<ApiProperty, string>>> LookupFlightsByAircraftAsync(
            string address,
            DateTime date)
        {
            List<Dictionary<ApiProperty, string>> properties = [];

            Factory.Logger.LogMessage(Severity.Info, $"Looking up flights for aircraft with address {address} at {date}");

            // Convert the date to UTC and generate a representation in the required format
            var fromDate = date.ToUniversalTime().AddDays(-1).ToString("yyyy-MM-dd");
            var toDate = date.ToUniversalTime().AddDays(1).ToString("yyyy-MM-dd");

            // Log the request
            var url = $"{_baseAddress}{address}/{fromDate}/{toDate}";
            await Factory.ApiLogManager.AddAsync(ServiceType, ApiEndpointType.HistoricalFlights, url, ApiProperty.AircraftAddress, address);

            // Make a request for the data from the API
            var node = await GetAsync(ServiceType, url, new Dictionary<string, string>()
            {
                { "X-RapidAPI-Key", _key },
                { "X-RapidAPI-Host", _host },
            });

            var array = node as JsonArray;
            if (array != null)
            {
                // Iterate over each (presumed) flight in the response
                foreach (var flight in array)
                {
                    // Extract the flight properties into a dictionary and add them to the collection
                    // of flight property dictionaries
                    var flightProperties = ExtractSingleFlight(flight);
                    properties.Add(flightProperties);
                }
            }

            return properties.Count > 0 ? properties : null;
        }

        /// <summary>
        /// Extract properties for a single flight into a dictionary
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        private Dictionary<ApiProperty, string> ExtractSingleFlight(JsonNode node)
        {
            // Extract the properties of interest from the node
            var flightIATA = GetStringValue(node, "number").Replace(" ", "");
            Dictionary<ApiProperty, string> properties = new()
            {
                { ApiProperty.FlightIATA, flightIATA },
                { ApiProperty.FlightICAO, "" },
                { ApiProperty.ModelICAO, "" }
            };

            // Get the nodes for point of embarkation, destination, airline and aircraft and add those
            // to the dictionary
            ExtractEmbarkationAirport(node, properties);
            ExtractDestinationAirport(node, properties);
            ExtractAirline(node, properties);
            ExtractAircraft(node, properties);

            // Log the properties dictionary
            LogProperties("Flight", properties);

            return properties;
        }

        /// <summary>
        /// Extract the properties of the embarkation airport
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        private void ExtractEmbarkationAirport(JsonNode node, Dictionary<ApiProperty, string> properties)
        {
            // Find the departure airport node and the departure time node. For the latter, try "runwayTime" first
            // and if that's not there fallback to "scheduledTime"
            var departure = GetObjectValue(node, "departure") as JsonObject;
            var airport = GetObjectValue(departure, "airport") as JsonObject;
            var time = GetObjectValue(departure, "runwayTime") as JsonObject;
            time ??= GetObjectValue(departure, "revisedTime") as JsonObject;
            time ??= GetObjectValue(departure, "scheduledTime") as JsonObject;

            Factory.Logger.LogMessage(Severity.Debug, $"Extracting destination airport details from {airport?.ToJsonString()}");
            Factory.Logger.LogMessage(Severity.Debug, $"Extracting departure time from {time?.ToJsonString()}");

            // Extract the properties of interest from the node
            properties.Add(ApiProperty.EmbarkationIATA, GetStringValue(airport, "iata"));
            properties.Add(ApiProperty.DepartureTime, GetStringValue(time, "utc"));
        }

        /// <summary>
        /// Extract the properties of the destination airport
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        private void ExtractDestinationAirport(JsonNode node, Dictionary<ApiProperty, string> properties)
        {
            // Find the arrival airport node and the departure time node. For the latter, try "runwayTime" first
            // and if that's not there fallback to "scheduledTime"
            var arrival = GetObjectValue(node, "arrival") as JsonObject;
            var airport = GetObjectValue(arrival, "airport") as JsonObject;
            var time = GetObjectValue(arrival, "revisedTime") as JsonObject;
            time ??= GetObjectValue(arrival, "predictedTime") as JsonObject;
            time ??= GetObjectValue(arrival, "scheduledTime") as JsonObject;

            Factory.Logger.LogMessage(Severity.Debug, $"Extracting arrival airport details from {airport?.ToJsonString()}");
            Factory.Logger.LogMessage(Severity.Debug, $"Extracting arrival time from {time?.ToJsonString()}");

            // Extract the properties of interest from the node
            properties.Add(ApiProperty.DestinationIATA, GetStringValue(airport, "iata"));
            properties.Add(ApiProperty.ArrivalTime, GetStringValue(time, "utc"));
        }

        /// <summary>
        /// Extract the properties of the airline
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        private void ExtractAirline(JsonNode node, Dictionary<ApiProperty, string> properties)
        {
            // Find the airline node
            var airline = GetObjectValue(node, "airline") as JsonObject;
            Factory.Logger.LogMessage(Severity.Debug, $"Extracting airline details from {airline?.ToJsonString()}");

            // Extract the properties of interest from the node
            properties.Add(ApiProperty.AirlineName, GetStringValue(airline, "name"));
            properties.Add(ApiProperty.AirlineIATA, GetStringValue(airline, "iata"));
            properties.Add(ApiProperty.AirlineICAO, GetStringValue(airline, "icao"));
        }

        /// <summary>
        /// Extract the properties of the aircraft
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        private void ExtractAircraft(JsonNode node, Dictionary<ApiProperty, string> properties)
        {
            // Find the airline node
            var aircraft = GetObjectValue(node, "aircraft") as JsonObject;
            Factory.Logger.LogMessage(Severity.Debug, $"Extracting aircraft details from {aircraft?.ToJsonString()}");

            // Extract the properties of interest from the node
            properties.Add(ApiProperty.AircraftRegistration, GetStringValue(aircraft, "reg"));
            properties.Add(ApiProperty.AircraftAddress, GetStringValue(aircraft, "modeS"));
        }
    }
}
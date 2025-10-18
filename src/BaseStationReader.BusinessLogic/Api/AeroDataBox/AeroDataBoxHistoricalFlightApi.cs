using System.Text.Json.Nodes;
using BaseStationReader.Entities.Config;
using BaseStationReader.Entities.Logging;
using BaseStationReader.Entities.Api;
using BaseStationReader.Interfaces.Api;
using System.Diagnostics.CodeAnalysis;
using BaseStationReader.Interfaces.Database;

namespace BaseStationReader.BusinessLogic.Api.AirLabs
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
            Factory.Logger.LogMessage(Severity.Info, $"Looking up flights for aircraft with address {address} at {date}");
            var properties = await MakeApiRequestAsync(address, date);
            return properties;
        }

        /// <summary>
        /// Make a request to the specified URL
        /// </summary>
        /// <param name="parameters"></param>
        /// <returns></returns>
        private async Task<List<Dictionary<ApiProperty, string>>> MakeApiRequestAsync(string address, DateTime date)
        {
            List<Dictionary<ApiProperty, string>> properties = [];

            // Convert the date to UTC and generate a representation in the required format
            var fromDate = date.ToUniversalTime().AddDays(-1).ToString("yyyy-MM-dd");
            var toDate = date.ToUniversalTime().AddDays(1).ToString("yyyy-MM-dd");

            // Make a request for the data from the API
            var url = $"{_baseAddress}{address}/{fromDate}/{toDate}";
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
            var flightIATA = (node?["number"]?.GetValue<string>() ?? "").Replace(" ", "");
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
            var airport = node?["departure"]?["airport"];
            var time = node?["departure"]?["runwayTime"];
            time ??= node?["departure"]?["revisedTime"];
            time ??= node?["departure"]?["scheduledTime"];

            Factory.Logger.LogMessage(Severity.Debug, $"Extracting destination airport details from {airport?.ToJsonString()}");
            Factory.Logger.LogMessage(Severity.Debug, $"Extracting departure time from {time?.ToJsonString()}");

            // Extract the properties of interest from the node
            properties.Add(ApiProperty.EmbarkationIATA, airport?["iata"]?.GetValue<string>() ?? "");
            properties.Add(ApiProperty.DepartureTime, time?["utc"]?.GetValue<string>() ?? "");
        }

        /// <summary>
        /// Extract the properties of the destination airport
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        private void ExtractDestinationAirport(JsonNode node, Dictionary<ApiProperty, string> properties)
        {
            // Find the arrival airport node and the arrival time node. For the latter, use revised time, predicted
            // time and scheduled time, in thatr order
            var airport = node?["arrival"]?["airport"];
            var time = node?["arrival"]?["revisedTime"];
            time ??= node?["arrival"]?["predictedTime"];
            time ??= node?["arrival"]?["scheduledTime"];

            Factory.Logger.LogMessage(Severity.Debug, $"Extracting arrival airport details from {airport?.ToJsonString()}");
            Factory.Logger.LogMessage(Severity.Debug, $"Extracting arrival time from {time?.ToJsonString()}");

            // Extract the properties of interest from the node
            properties.Add(ApiProperty.DestinationIATA, airport?["iata"]?.GetValue<string>() ?? "");
            properties.Add(ApiProperty.ArrivalTime, time?["utc"]?.GetValue<string>() ?? "");
        }

        /// <summary>
        /// Extract the properties of the airline
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        private void ExtractAirline(JsonNode node, Dictionary<ApiProperty, string> properties)
        {
            // Find the airline node
            var airline = node?["airline"];
            Factory.Logger.LogMessage(Severity.Debug, $"Extracting airline details from {airline?.ToJsonString()}");

            // Extract the properties of interest from the node
            properties.Add(ApiProperty.AirlineName, airline?["name"]?.GetValue<string>() ?? "");
            properties.Add(ApiProperty.AirlineIATA, airline?["iata"]?.GetValue<string>() ?? "");
            properties.Add(ApiProperty.AirlineICAO, airline?["icao"]?.GetValue<string>() ?? "");
        }

        /// <summary>
        /// Extract the properties of the aircraft
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        private void ExtractAircraft(JsonNode node, Dictionary<ApiProperty, string> properties)
        {
            // Find the airline node
            var aircraft = node?["aircraft"];
            Factory.Logger.LogMessage(Severity.Debug, $"Extracting aircraft details from {aircraft?.ToJsonString()}");

            // Extract the properties of interest from the node
            properties.Add(ApiProperty.AircraftRegistration, aircraft?["reg"]?.GetValue<string>() ?? "");
            properties.Add(ApiProperty.AircraftAddress, aircraft?["modeS"]?.GetValue<string>() ?? "");
        }
    }
}
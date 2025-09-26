using System.Text.Json.Nodes;
using BaseStationReader.Entities.Interfaces;
using BaseStationReader.Entities.Logging;
using BaseStationReader.Entities.Lookup;

namespace BaseStationReader.BusinessLogic.Api.AirLabs
{
    public class AeroDataBoxHistoricalFlightApi : ExternalApiBase, IHistoricalFlightApi
    {
        private readonly string _baseAddress;
        private readonly string _host;
        private readonly string _key;

        public AeroDataBoxHistoricalFlightApi(
            ITrackerLogger logger,
            ITrackerHttpClient client,
            string url,
            string key) : base(logger, client)
        {
            _baseAddress = $"{url}/flights/icao24";
            _key = key;

            // Extract the host from the url
            var uri = new Uri(url);
            _host = uri.Host;
        }

        /// <summary>
        /// Lookup flight details using a date and time
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public async Task<List<Dictionary<ApiProperty, string>>> LookupFlightsByAircraftAsync(string address)
        {
            Logger.LogMessage(Severity.Info, $"Looking up flights for aircraft with address {address}");
            var properties = await MakeApiRequestAsync(address);
            return properties;
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
            var node = await SendRequestAsync(url, new Dictionary<string, string>()
            {
                { "X-RapidAPI-Key", _key },
                { "X-RapidAPI-Host", _host },
            });

            if (node != null)
            {
                try
                {
                    // Iterate over each (presumed) flight in the response
                    foreach (var flight in node as JsonArray)
                    {
                        // Extract the flight properties into a dictionary and add them to the collection
                        // of flight property dictionaries
                        var flightProperties = ExtractSingleFlight(flight);
                        properties.Add(flightProperties);
                    }
                }
                catch (Exception ex)
                {
                    var message = $"Error processing response: {ex.Message}";
                    Logger.LogMessage(Severity.Error, message);
                    Logger.LogException(ex);
                    properties = [];
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
            var flightNumber = (node!["number"]?.GetValue<string>() ?? "").Replace(" ", "");
            Dictionary<ApiProperty, string> properties = new()
            {
                { ApiProperty.FlightNumber, flightNumber }
            };

            // Get the nodes for point of embarkation, destination and airline and add those
            // to the dictionary
            ExtractEmbarkationAirport(node, properties);
            ExtractDestinationAirport(node, properties);
            ExtractAirline(node, properties);

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
            // Find the departure airport node
            var airport = node!["departure"]!["airport"];
            var time = node!["departure"]!["runwayTime"];

            Logger.LogMessage(Severity.Debug, $"Extracting destination airport details from {airport?.ToJsonString()}");
            Logger.LogMessage(Severity.Debug, $"Extracting departure time from {time?.ToJsonString()}");


            // Extract the properties of interest from the node
            properties.Add(ApiProperty.EmbarkationIATA, airport!["iata"]?.GetValue<string>() ?? "");
            properties.Add(ApiProperty.DepartureTime, time!["utc"]?.GetValue<string>() ?? "");
        }

        /// <summary>
        /// Extract the properties of the destination airport
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        private void ExtractDestinationAirport(JsonNode node, Dictionary<ApiProperty, string> properties)
        {
            // Find the arrival airport node
            var airport = node!["arrival"]!["airport"];
            var time = node!["arrival"]!["revisedTime"];

            Logger.LogMessage(Severity.Debug, $"Extracting arrival airport details from {airport?.ToJsonString()}");
            Logger.LogMessage(Severity.Debug, $"Extracting arrival time from {time?.ToJsonString()}");

            // Extract the properties of interest from the node
            properties.Add(ApiProperty.DestinationIATA, airport!["iata"]?.GetValue<string>() ?? "");
            properties.Add(ApiProperty.ArrivalTime, time!["utc"]?.GetValue<string>() ?? "");
        }

        /// <summary>
        /// Extract the properties of the airline
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        private void ExtractAirline(JsonNode node, Dictionary<ApiProperty, string> properties)
        {
            // Find the airlien node
            var airline = node!["airline"];
            Logger.LogMessage(Severity.Debug, $"Extracting airline details from {airline?.ToJsonString()}");

            // Extract the properties of interest from the node
            properties.Add(ApiProperty.AirlineName, airline!["name"]?.GetValue<string>() ?? "");
            properties.Add(ApiProperty.AirlineIATA, airline!["iata"]?.GetValue<string>() ?? "");
            properties.Add(ApiProperty.AirlineICAO, airline!["icao"]?.GetValue<string>() ?? "");
        }
    }
}
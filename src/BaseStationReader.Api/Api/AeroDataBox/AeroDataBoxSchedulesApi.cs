using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;
using BaseStationReader.Entities.Api;
using BaseStationReader.Entities.Config;
using BaseStationReader.Entities.Logging;
using BaseStationReader.Interfaces.Api;
using BaseStationReader.Interfaces.Database;

namespace BaseStationReader.Api.AeroDatabox
{
    internal class AeroDataBoxSchedulesApi : ExternalApiBase, ISchedulesApi
    {
        private const ApiServiceType ServiceType = ApiServiceType.AeroDataBox;
        private const string DateTimeFormat = "yyyy-MM-ddTHH:mm";
        private const int TimespanLimitHours = 12;

        private readonly string _baseAddress;
        private readonly string _host;
        private readonly string _key;

        private static readonly Dictionary<string, string> _queryParameters = new()
        {
            { "withLeg", "false" },
            { "direction", "both" },
            { "withCancelled", "true" },
            { "withCodeshared", "true" },
            { "withCargo", "true" },
            { "withPrivate", "false" },
            { "withLocation", "false" },
            { "codeType", "iata" }
        };

        [ExcludeFromCodeCoverage]
        public AeroDataBoxSchedulesApi(
            ITrackerHttpClient client,
            IDatabaseManagementFactory factory,
            ExternalApiSettings settings) : base(client, factory)
        {
            // Get the API configuration properties and store the key
            var definition = settings.ApiServices.FirstOrDefault(x => x.Service == ServiceType);
            _key = definition?.Key;

            // Get the endpoint URL, set up the base address for requests and extract the host name
            var url = definition.ApiEndpoints.FirstOrDefault(x => x.EndpointType == ApiEndpointType.Schedules)?.Url;
            _baseAddress = url;
            _host = new Uri(url).Host;

            // Set the rate limit for this service on the HTTP client
            client.SetRateLimits(ServiceType, definition?.RateLimit ?? 0);
        }

        /// <summary>
        /// Lookup and return JSON scheduling information for an airport in a time range
        /// </summary>
        /// <param name="iata"></param>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <returns></returns>
        public async Task<JsonNode> LookupSchedulesRawAsync(string iata, DateTime from, DateTime to)
        {
            // Make sure the timespan represented by the two dates is valid
            var timespan = (int)Math.Round((to - from).TotalMinutes, 0, MidpointRounding.AwayFromZero);
            if (timespan < 1)
            {
                Factory.Logger.LogMessage(Severity.Error, $"{from} to {to} gives a timespan of {timespan} minutes which is invalid");
                return null;
            }

            // Make sure the timespan represented by the two dates is within the allowed range
            if (timespan > 60 * TimespanLimitHours)
            {
                Factory.Logger.LogMessage(Severity.Error, $"{from} to {to} gives a timespan of {timespan} minutes which is too large");
                return null;
            }

            // Construct date representations of the dates
            var fromStr = from.ToString(DateTimeFormat);
            var toStr = to.ToString(DateTimeFormat);

            // Construct the URL with query parameters and log the request
            var url = $"{_baseAddress}/{iata}/{fromStr}/{toStr}?{BuildQueryString()}";
            await Factory.ApiLogManager.AddAsync(ServiceType, ApiEndpointType.Schedules, url, ApiProperty.AirportIATA, iata);

            // Make a request for the data from the API
            JsonNode node = await GetAsync(ServiceType, url, new Dictionary<string, string>()
            {
                { "X-RapidAPI-Key", _key },
                { "X-RapidAPI-Host", _host },
            });

            return node;
        }

        /// <summary>
        /// Extracts flight IATA code mappings from an airport schedule response.
        /// </summary>
        /// <param name="schedules">The JSON returned by <see cref="LookupSchedulesRawAsync"/>.</param>
        /// <param name="airportIata">The IATA code of the airport whose schedule was requested.</param>
        /// <returns>All schedule rows projected as flight mapping entities.</returns>
        public List<FlightIATACodeMapping> ExtractFlightMapping(JsonNode schedules, string airportIata)
        {
            var mappings = new List<FlightIATACodeMapping>();
            if (schedules is null || string.IsNullOrWhiteSpace(airportIata)) return mappings;

            var scheduleObject = schedules as JsonObject;
            if (scheduleObject is null) return mappings;

            ExtractFlightMappings(scheduleObject["departures"] as JsonArray, AirportType.Departure,
                airportIata.Trim().ToUpperInvariant(), mappings);
            ExtractFlightMappings(scheduleObject["arrivals"] as JsonArray, AirportType.Arrival,
                airportIata.Trim().ToUpperInvariant(), mappings);

            // Preserve every provider row and its direction for display and complete CSV export.
            return mappings;
        }

        /// <summary>
        /// Extracts mappings for one direction from an AeroDataBox schedule collection.
        /// </summary>
        private static void ExtractFlightMappings(JsonArray flights, AirportType airportType,
            string scheduleAirportIata, ICollection<FlightIATACodeMapping> mappings)
        {
            if (flights is null) return;

            foreach (var flight in flights.OfType<JsonObject>())
            {
                var airport = flight["movement"]?["airport"];
                var airline = flight["airline"];
                var flightIata = Compact(flight["number"]);
                var callsign = Compact(flight["callSign"]);
                var airlineIata = Compact(airline?["iata"]);
                var airlineIcao = Compact(airline?["icao"]);

                var remoteAirportIata = Compact(airport?["iata"]);
                mappings.Add(new FlightIATACodeMapping
                {
                    AirlineICAO = airlineIcao,
                    AirlineIATA = airlineIata,
                    AirlineName = Text(airline?["name"]),
                    AirportICAO = Compact(airport?["icao"]),
                    AirportIATA = remoteAirportIata,
                    AirportName = Text(airport?["name"]),
                    AirportType = airportType,
                    Embarkation = airportType == AirportType.Departure ? scheduleAirportIata : remoteAirportIata,
                    Destination = airportType == AirportType.Arrival ? scheduleAirportIata : remoteAirportIata,
                    FlightIATA = flightIata,
                    Callsign = callsign,
                    FileName = "AeroDataBox"
                });
            }
        }

        /// <summary>
        /// Returns trimmed JSON text with embedded spaces removed.
        /// </summary>
        private static string Compact(JsonNode node)
            => Text(node).Replace(" ", string.Empty);

        /// <summary>
        /// Returns trimmed JSON text or an empty string when the value is absent.
        /// </summary>
        private static string Text(JsonNode node)
            => node?.GetValue<string>()?.Trim() ?? string.Empty;

        /// <summary>
        /// Build the query string from the fixed parameters
        /// </summary>
        /// <returns></returns>
        private static string BuildQueryString()
            => string.Join("&", _queryParameters.Select(x => $"{x.Key}={x.Value}"));
    }
}

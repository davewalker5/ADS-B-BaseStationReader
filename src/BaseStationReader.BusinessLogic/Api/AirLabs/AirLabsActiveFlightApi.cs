using System.Text.Json.Nodes;
using BaseStationReader.BusinessLogic.Geometry;
using BaseStationReader.Entities.Config;
using BaseStationReader.Entities.Geometry;
using BaseStationReader.Interfaces.Logging;
using BaseStationReader.Entities.Logging;
using BaseStationReader.Entities.Api;
using BaseStationReader.Interfaces.Api;
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

        public AirLabsActiveFlightApi(
            ITrackerLogger logger,
            ITrackerHttpClient client,
            IDatabaseManagementFactory factory,
            ExternalApiSettings settings) : base(logger, client, factory)
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
        public async Task<Dictionary<ApiProperty, string>> LookupFlight(ApiProperty propertyType, string propertyValue)
        {
            Logger.LogMessage(Severity.Info, $"Looking up active flight using {propertyType} {propertyValue}");
            var properties = await MakeApiRequestAsync($"&hex={propertyValue}");
            return properties.Count > 0 ? properties.First() : null;
        }

        /// <summary>
        /// Lookup all active flights within a bounding box around a central point
        /// </summary>
        /// <param name="centreLatitude"></param>
        /// <param name="centreLongitude"></param>
        /// <param name="rangeNm"></param>
        /// <returns></returns>
        public async Task<List<Dictionary<ApiProperty, string>>> LookupFlightsInBoundingBox(
            double centreLatitude,
            double centreLongitude,
            double rangeNm)
        {
            Logger.LogMessage(Severity.Info, $"Looking for active flights in a {rangeNm} Nm bounding box around ({centreLatitude}, {centreLongitude})");

            // Convert the range to metres and calculate the bounding box
            var rangeMetres = 1852.0 * rangeNm;
            (_, Coordinate northEast, _, Coordinate southWest) =
                CoordinateMathematics.GetBoundingBox(centreLatitude, centreLongitude, rangeMetres);

            // Make the API request and parse the response to yield a list of flight property dicitonaries 
            var properties = await MakeApiRequestAsync($"&bbox={southWest.Latitude},{southWest.Longitude},{northEast.Latitude},{northEast.Longitude}");
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
            var node = await GetAsync(Logger, ServiceType, url, []);

            try
            {
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
            }
            catch (Exception ex)
            {
                Logger.LogMessage(Severity.Error, ex.Message);
                Logger.LogException(ex);
                properties = [];
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
            // Get the flight number and airline IATA code and combine them to produce a recognisable flight
            // number (this is also the flight IATA)
            var flightNumberOnly = node?["flight_number"]?.GetValue<string>() ?? "";
            var airlineIATA = node?["airline_iata"]?.GetValue<string>() ?? "";
            var flightNumber = ((flightNumberOnly != "") && (airlineIATA != "") && !flightNumberOnly.StartsWith(airlineIATA)) ?
                $"{airlineIATA}{flightNumberOnly}" : flightNumberOnly;

            // Extract the properties of interest from the node
            Dictionary<ApiProperty, string> properties = new()
            {
                { ApiProperty.EmbarkationIATA, node?["dep_iata"]?.GetValue<string>() ?? "" },
                { ApiProperty.DestinationIATA, node?["arr_iata"]?.GetValue<string>() ?? "" },
                { ApiProperty.FlightIATA, node?["flight_iata"]?.GetValue<string>() ?? "" },
                { ApiProperty.FlightICAO, node?["flight_icao"]?.GetValue<string>() ?? "" },
                { ApiProperty.FlightNumber, flightNumber },
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
    }
}

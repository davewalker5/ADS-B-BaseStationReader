using System.Text.Json.Nodes;
using BaseStationReader.BusinessLogic.Geometry;
using BaseStationReader.Entities.Geometry;
using BaseStationReader.Entities.Interfaces;
using BaseStationReader.Entities.Logging;
using BaseStationReader.Entities.Lookup;

namespace BaseStationReader.BusinessLogic.Api.AirLabs
{
    public class AirLabsActiveFlightApi : ExternalApiBase, IActiveFlightApi
    {
        private readonly string _baseAddress;

        public AirLabsActiveFlightApi(ITrackerLogger logger, ITrackerHttpClient client, string url, string key) : base(logger, client)
        {
            _baseAddress = $"{url}?api_key={key}";
        }

        /// <summary>
        /// Lookup an active flight's details using the aircraft's ICAO 24-bit address
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public async Task<Dictionary<ApiProperty, string>> LookupFlightByAircraftAsync(string address)
        {
            Logger.LogMessage(Severity.Info, $"Looking up active flight for aircraft with address {address}");
            var properties = await MakeApiRequestAsync($"&hex={address}");
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
            var node = await SendRequestAsync(url);

            if (node != null)
            {
                try
                {
                    // Extract the response element from the JSON DOM as a JSON array
                    var apiResponse = node!["response"] as JsonArray;

                    // Iterate over each (presumed) flight in the response
                    foreach (var flight in apiResponse)
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

            return properties;
        }

        /// <summary>
        /// Extract properties for a single flight into a dictionary
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        private Dictionary<ApiProperty, string> ExtractSingleFlight(JsonNode node)
        {
            // Get the flight number and airline IATA code and combine them to produce a recognisable flight
            // number (this is also the flight IATA)
            var flightNumberOnly = node!["flight_number"]?.GetValue<string>() ?? "";
            var airlineIATA = node!["airline_iata"]?.GetValue<string>() ?? "";
            var flightNumber = ((flightNumberOnly != "") && (airlineIATA != "") && !flightNumberOnly.StartsWith(airlineIATA)) ?
                $"{airlineIATA}{flightNumberOnly}" : flightNumberOnly;

            // Extract the properties of interest from the node
            Dictionary<ApiProperty, string> properties = new()
            {
                { ApiProperty.EmbarkationIATA, node!["dep_iata"]?.GetValue<string>() ?? "" },
                { ApiProperty.DestinationIATA, node!["arr_iata"]?.GetValue<string>() ?? "" },
                { ApiProperty.FlightIATA, node!["flight_iata"]?.GetValue<string>() ?? "" },
                { ApiProperty.FlightICAO, node!["flight_icao"]?.GetValue<string>() ?? "" },
                { ApiProperty.FlightNumber, flightNumber },
                { ApiProperty.AirlineIATA, airlineIATA },
                { ApiProperty.AirlineICAO, node!["airline_icao"]?.GetValue<string>() ?? "" },
                { ApiProperty.AirlineName, "" },
                { ApiProperty.ModelICAO, node!["aircraft_icao"]?.GetValue<string>() ?? "" },
                { ApiProperty.AircraftAddress, node!["hex"]?.GetValue<string>() ?? "" },
            };

            // Log the properties dictionary
            LogProperties("Flight", properties);

            return properties;
        }
    }
}

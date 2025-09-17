using BaseStationReader.Entities.Interfaces;
using BaseStationReader.Entities.Logging;
using BaseStationReader.Entities.Tracking;

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
            return properties;
        }

        /// <summary>
        /// Make a request to the specified URL
        /// </summary>
        /// <param name="parameters"></param>
        /// <returns></returns>
        private async Task<Dictionary<ApiProperty, string>> MakeApiRequestAsync(string parameters)
        {
            Dictionary<ApiProperty, string> properties = null;

            // Make a request for the data from the API
            var url = $"{_baseAddress}{parameters}";
            var node = await SendRequestAsync(url);

            if (node != null)
            {
                try
                {
                    // Extract the response element from the JSON DOM
                    var apiResponse = node!["response"]![0];

                    // Extract the values into a dictionary
                    properties = new()
                    {
                        { ApiProperty.DepartureAirportIATA, apiResponse!["dep_iata"]?.GetValue<string>() ?? "" },
                        { ApiProperty.DepartureAirportICAO, apiResponse!["dep_icao"]?.GetValue<string>() ?? "" },
                        { ApiProperty.DestinationAirportIATA, apiResponse!["arr_iata"]?.GetValue<string>() ?? "" },
                        { ApiProperty.DestinationAirportICAO, apiResponse!["arr_icao"]?.GetValue<string>() ?? "" },
                        { ApiProperty.FlightIATA, apiResponse!["flight_iata"]?.GetValue<string>() ?? "" },
                        { ApiProperty.FlightICAO, apiResponse!["flight_icao"]?.GetValue<string>() ?? "" }
                    };

                    // Log the properties dictionary
                    LogProperties(properties!);
                }
                catch (Exception ex)
                {
                    var message = $"Error processing response: {ex.Message}";
                    Logger.LogMessage(Severity.Error, message);
                    Logger.LogException(ex);
                    properties = null;
                }
            }

            return properties;
        }
    }
}

using BaseStationReader.Entities.Interfaces;
using BaseStationReader.Entities.Logging;
using BaseStationReader.Entities.Tracking;

namespace BaseStationReader.BusinessLogic.Api.AirLabs
{
    public class AirLabsAircraftApi : ExternalApiBase, IAircraftApi
    {
        private readonly string _baseAddress;

        public AirLabsAircraftApi(ITrackerLogger logger, ITrackerHttpClient client, string url, string key) : base(logger, client)
        {
            _baseAddress = $"{url}?api_key={key}";
        }

        /// <summary>
        /// Lookup an aircraft's details using its ICAO 24-bit address
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public async Task<Dictionary<ApiProperty, string>> LookupAircraftAsync(string address)
        {
            Logger.LogMessage(Severity.Info, $"Looking up aircraft with address {address}");
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
                        { ApiProperty.AirlineIATA, apiResponse!["airline_iata"]?.GetValue<string>() ?? "" },
                        { ApiProperty.AirlineICAO, apiResponse!["airline_icao"]?.GetValue<string>() ?? "" },
                        { ApiProperty.ManufacturerName, apiResponse!["manufacturer"]?.GetValue<string>() ?? "" },
                        { ApiProperty.ModelIATA, apiResponse!["iata"]?.GetValue<string>() ?? "" },
                        { ApiProperty.ModelICAO, apiResponse!["icao"]?.GetValue<string>() ?? "" }
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

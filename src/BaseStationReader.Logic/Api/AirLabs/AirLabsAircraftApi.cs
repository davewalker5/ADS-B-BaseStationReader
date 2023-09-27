using BaseStationReader.Entities.Interfaces;
using BaseStationReader.Entities.Tracking;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;

namespace BaseStationReader.Logic.Api.AirLabs
{
    [ExcludeFromCodeCoverage]
    public class AirLabsAircraftApi : IAircraftApi
    {
        private readonly HttpClient _client = new();
        private readonly string _baseAddress;

        public AirLabsAircraftApi(string url, string key)
        {
            _baseAddress = $"{url}?api_key={key}";
        }

        /// <summary>
        /// Lookup an aircraft's details using its ICAO 24-bit address
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public async Task<Dictionary<ApiProperty, string>?> LookupAircraft(string address)
        {
            return await MakeApiRequest($"&hex={address}");
        }

        /// <summary>
        /// Make a request to the specified URL
        /// </summary>
        /// <param name="parameters"></param>
        /// <returns></returns>
        private async Task<Dictionary<ApiProperty, string>?> MakeApiRequest(string parameters)
        {
            Dictionary<ApiProperty, string>? properties = null;

            // Make a request for the data from the API
            var url = $"{_baseAddress}{parameters}";
            using (var response = await _client.GetAsync(url))
            {
                if (response.IsSuccessStatusCode)
                {
                    try
                    {
                        // Read the response, parse to a JSON DOM and extract the values
                        var json = await response.Content.ReadAsStringAsync();
                        var node = JsonNode.Parse(json);
                        var apiResponse = node!["response"]![0];

                        // Extract the values into a dictionary
                        properties = new()
                        {
                            { ApiProperty.AirlineIATA, apiResponse!["airline_iata"]?.GetValue<string?>() ?? "" },
                            { ApiProperty.AirlineICAO, apiResponse!["airline_icao"]?.GetValue<string?>() ?? "" },
                            { ApiProperty.ManufacturerName, apiResponse!["manufacturer"]?.GetValue<string>() ?? "" },
                            { ApiProperty.ModelIATA, apiResponse!["iata"]?.GetValue<string>() ?? "" },
                            { ApiProperty.ModelICAO, apiResponse!["icao"]?.GetValue<string>() ?? "" }
                        };
                    }
                    catch
                    {
                        properties = null;
                    }
                }
            }

            return properties;
        }
    }
}

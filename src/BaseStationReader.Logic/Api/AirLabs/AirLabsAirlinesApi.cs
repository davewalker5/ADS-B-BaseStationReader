using BaseStationReader.Entities.Interfaces;
using BaseStationReader.Entities.Tracking;

namespace BaseStationReader.Logic.Api.AirLabs
{
    public class AirLabsAirlinesApi : ExternalApiBase, IAirlinesApi
    {
        private readonly string _baseAddress;

        public AirLabsAirlinesApi(ITrackerHttpClient client, string url, string key) : base(client)
        {
            _baseAddress = $"{url}?api_key={key}";
        }

        /// <summary>
        /// Lookup an airline using its IATA code
        /// </summary>
        /// <param name="iata"></param>
        /// <returns></returns>
        public async Task<Dictionary<ApiProperty, string>?> LookupAirlineByIATACode(string iata)
        {
            return await MakeApiRequest($"&iata_code={iata}");
        }

        /// <summary>
        /// Lookup an airline using it's ICAO code
        /// </summary>
        /// <param name="icao"></param>
        /// <returns></returns>
        public async Task<Dictionary<ApiProperty, string>?> LookupAirlineByICAOCode(string icao)
        {
            return await MakeApiRequest($"&icao_code={icao}");
        }

        /// <summary>
        /// Make a request to the specified URL and return the response properties as a dictionary
        /// </summary>
        /// <param name="parameters"></param>
        /// <returns></returns>
        private async Task<Dictionary<ApiProperty, string>?> MakeApiRequest(string parameters)
        {
            Dictionary<ApiProperty, string>? properties = null;

            // Make a request for the data from the API
            var url = $"{_baseAddress}{parameters}";
            var node = await SendRequest(url);

            if (node != null)
            {
                try
                {
                    // Extract the response element from the JSON DOM
                    var apiResponse = node!["response"]![0];

                    // Extract the values into a dictionary
                    properties = new()
                    {
                        { ApiProperty.AirlineIATA, apiResponse!["iata_code"]!.GetValue<string>() },
                        { ApiProperty.AirlineICAO, apiResponse!["icao_code"]!.GetValue<string>() },
                        { ApiProperty.AirlineName, apiResponse!["name"]!.GetValue<string>() },
                    };
                }
                catch
                {
                    properties = null;
                }
            }

            return properties;
        }
    }
}

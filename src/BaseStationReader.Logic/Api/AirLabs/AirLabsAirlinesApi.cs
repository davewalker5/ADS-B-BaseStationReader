﻿using BaseStationReader.Entities.Interfaces;
using BaseStationReader.Entities.Tracking;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;

namespace BaseStationReader.Logic.Api.AirLabs
{
    [ExcludeFromCodeCoverage]
    public class AirLabsAirlinesApi : IAirlinesApi
    {
        private readonly HttpClient _client = new();
        private readonly string _baseAddress;

        public AirLabsAirlinesApi(string url, string key)
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
            using (var response = await _client.GetAsync(url))
            {
                if (response.IsSuccessStatusCode)
                {
                    try
                    {
                        // Read the response, parse to a JSON DOM
                        var json = await response.Content.ReadAsStringAsync();
                        var node = JsonNode.Parse(json);
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
            }

            return properties;
        }
    }
}

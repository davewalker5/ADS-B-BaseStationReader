﻿using BaseStationReader.Entities.Interfaces;
using BaseStationReader.Entities.Tracking;

namespace BaseStationReader.Logic.Api.AirLabs
{
    public class AirLabsAircraftApi : ExternalApiBase, IAircraftApi
    {
        private readonly string _baseAddress;

        public AirLabsAircraftApi(ITrackerHttpClient client, string url, string key) : base(client)
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

            return properties;
        }
    }
}

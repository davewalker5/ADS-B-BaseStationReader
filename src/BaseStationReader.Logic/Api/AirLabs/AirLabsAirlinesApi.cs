using BaseStationReader.Entities.Interfaces;
using BaseStationReader.Entities.Lookup;
using BaseStationReader.Logic.Api.Base;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;

namespace BaseStationReader.Logic.Api.AirLabs
{
    [ExcludeFromCodeCoverage]
    public class AirLabsAirlinesApi : AirlineApiBase, IAirlinesApi
    {
        private readonly HttpClient _client = new();
        private readonly string _baseAddress;

        public AirLabsAirlinesApi(IAirlineManager manager, string url, string key) : base(manager)
        {
            _baseAddress = $"{url}?api_key={key}";
        }

        /// <summary>
        /// Lookup an airline using its IATA code
        /// </summary>
        /// <param name="iata"></param>
        /// <returns></returns>
        public async override Task<Airline?> LookupAirlineByIATACode(string iata)
        {
            // Use the base method, first, as this will retrieve locally cached information
            Airline? airline = await base.LookupAirlineByIATACode(iata);
            if (airline == null)
            {
                // Not found locally, so make a call to the API
                airline = await MakeApiRequest($"&iata_code={iata}");
            }

            return airline;
        }

        /// <summary>
        /// Lookup an airline using it's ICAO code
        /// </summary>
        /// <param name="icao"></param>
        /// <returns></returns>
        public async override Task<Airline?> LookupAirlineByICAOCode(string icao)
        {
            // Use the base method, first, as this will retrieve locally cached information
            Airline? airline = await base.LookupAirlineByICAOCode(icao);
            if (airline == null)
            {
                // Not found locally, so make a call to the API
                airline = await MakeApiRequest($"&icao_code={icao}");
            }

            return airline;
        }

        /// <summary>
        /// Make a request to the specified URL, that's expected to return a JSON representation of an
        /// airline
        /// </summary>
        /// <param name="parameters"></param>
        /// <returns></returns>
        private async Task<Airline?> MakeApiRequest(string parameters)
        {
            Airline? airline = null;

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
                        var iata = apiResponse!["iata_code"]!.GetValue<string>();
                        var icao = apiResponse!["icao_code"]!.GetValue<string>();
                        var name = apiResponse!["name"]!.GetValue<string>();

                        // Save the airline and return the airline object
                        airline = await base.WriteAirline(iata, icao, name);
                    }
                    catch
                    {
                        airline = null;
                    }
                }
            }

            return airline;
        }
    }
}

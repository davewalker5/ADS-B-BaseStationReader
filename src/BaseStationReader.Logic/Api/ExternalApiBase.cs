using BaseStationReader.Entities.Interfaces;
using System.Text.Json.Nodes;

namespace BaseStationReader.Logic.Api
{
    public abstract class ExternalApiBase
    {
        private readonly ITrackerHttpClient _client;

        protected ExternalApiBase(ITrackerHttpClient client)
        {
            _client = client;
        }

        /// <summary>
        /// Make a request to the specified URL and return the response properties as a JSON DOM
        /// </summary>
        /// <param name="endpoint"></param>
        /// <returns></returns>
        protected async Task<JsonNode?> SendRequest(string endpoint)
        {
            JsonNode? node = null;

            try
            {
                // Make a request for the data from the API
                using (var response = await _client.GetAsync(endpoint))
                {
                    // Check the request was successful
                    if (response.IsSuccessStatusCode)
                    {
                            // Read the response, parse to a JSON DOM
                            var json = await response.Content.ReadAsStringAsync();
                            node = JsonNode.Parse(json);
                    }
                }
            }
            catch
            {
                node = null;
            }

            return node;
        }
    }
}

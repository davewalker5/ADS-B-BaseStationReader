using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;

namespace BaseStationReader.Logic.Api
{
    [ExcludeFromCodeCoverage]
    public abstract class ExternalApiBase
    {
        protected readonly HttpClient _client = new();

        /// <summary>
        /// Make a request to the specified URL and return the response properties as a dictionary
        /// </summary>
        /// <param name="endpoint"></param>
        /// <returns></returns>
        protected async Task<JsonNode?> SendRequest(string endpoint)
        {
            JsonNode? node = null;

            // Make a request for the data from the API
            using (var response = await _client.GetAsync(endpoint))
            {
                // Check the request was successful
                if (response.IsSuccessStatusCode)
                {
                    try
                    {
                        // Read the response, parse to a JSON DOM
                        var json = await response.Content.ReadAsStringAsync();
                        node = JsonNode.Parse(json);
                    }
                    catch
                    {
                        node = null;
                    }
                }
            }

            return node;
        }
    }
}

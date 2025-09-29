using BaseStationReader.Entities.Config;
using BaseStationReader.Entities.Interfaces;
using BaseStationReader.Entities.Logging;
using BaseStationReader.Entities.Lookup;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;

namespace BaseStationReader.BusinessLogic.Api
{
    public abstract class ExternalApiBase
    {
        private readonly ITrackerHttpClient _client;

        protected ITrackerLogger Logger { get; private set; }

        protected ExternalApiBase(ITrackerLogger logger, ITrackerHttpClient client)
        {
            Logger = logger;
            _client = client;
        }

        /// <summary>
        /// Make a request to the specified URL and return the response properties as a JSON DOM
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="type"></param>
        /// <param name="endpoint"></param>
        /// <returns></returns>
        protected async Task<JsonNode> SendRequestAsync(ITrackerLogger logger, ApiServiceType type, string endpoint)
            => await SendRequestAsync(logger, type, endpoint, []);

        /// <summary>
        /// Make a request to the specified URL and return the response properties as a JSON DOM
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="type"></param>
        /// <param name="endpoint"></param>
        /// <param name="headers"></param>
        /// <returns></returns>
        protected async Task<JsonNode> SendRequestAsync(
            ITrackerLogger logger,
            ApiServiceType type,
            string endpoint,
            Dictionary<string, string> headers)
        {
            JsonNode node = null;

            try
            {
                Logger.LogMessage(Severity.Debug, $"Making request to {endpoint}");

                // Construct a request object, including the headers
                var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
                foreach (var header in headers)
                {
                    Logger.LogMessage(Severity.Debug, $"Adding header {header.Key}: {header.Value}");
                    request.Headers.Add(header.Key, header.Value);
                }

                // Make a request for the data from the API
                using (var response = await _client.SendAsync(logger, type, request))
                {
                    // Check the request was successful
                    if (response.IsSuccessStatusCode)
                    {
                        // Read the response, parse to a JSON DOM
                        var json = await response.Content.ReadAsStringAsync();
                        node = JsonNode.Parse(json);

                        Logger.LogMessage(Severity.Debug, $"Received response {json}");
                    }
                    else
                    {
                        Logger.LogMessage(Severity.Error, $"Response was not successful - code = {response.StatusCode}");
                    }
                }
            }
            catch (Exception ex)
            {
                var message = $"Error calling {endpoint}: {ex.Message}";
                Logger.LogMessage(Severity.Error, message);
                Logger.LogException(ex);
                node = null;
            }

            return node;
        }

        /// <summary>
        /// Log the content of a properties dictionary resulting from an external API call
        /// </summary>
        /// <param name="properties"></param>
        [ExcludeFromCodeCoverage]
        protected void LogProperties(string type, Dictionary<ApiProperty, string> properties)
        {
            // Check the properties dictionary isn't NULL
            if (properties != null)
            {
                // Not a NULL dictionary, so iterate over all the properties it contains
                foreach (var property in properties)
                {
                    // Construct a message containing the property name and the value, replacing
                    // null values with "NULL"
                    var value = property.Value != null ? property.Value.ToString() : "NULL";
                    var message = $"{type} API property {property.Key.ToString()} = {value}";

                    // Log the message for this property
                    Logger.LogMessage(Severity.Debug, message);
                }
            }
            else
            {
                // Log the fact that the properties dictionary is NULL
                Logger.LogMessage(Severity.Warning, "API lookup generated a NULL properties dictionary");
            }
        }
    }
}

using BaseStationReader.Entities.Config;
using BaseStationReader.Entities.Logging;
using BaseStationReader.Entities.Api;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json.Nodes;
using BaseStationReader.Interfaces.Api;
using System.Text.Json;
using BaseStationReader.Interfaces.Database;

namespace BaseStationReader.BusinessLogic.Api
{
    public abstract class ExternalApiBase
    {
        private readonly ITrackerHttpClient _client;

        protected IDatabaseManagementFactory Factory { get; private set; }

        protected ExternalApiBase(ITrackerHttpClient client, IDatabaseManagementFactory factory)
        {
            _client = client;
            Factory = factory;
        }

        /// <summary>
        /// Make a GET request to the specified URL and return the response as a JSON string
        /// </summary>
        /// <param name="type"></param>
        /// <param name="endpoint"></param>
        /// <param name="headers"></param>
        /// <returns></returns>
        protected async Task<JsonNode> GetAsync(
            ApiServiceType type,
            string endpoint,
            Dictionary<string, string> headers)
            => await SendRequestAsync(HttpMethod.Get, type, endpoint, headers, null);

        /// <summary>
        /// Make a request to the specified URL and return the response properties as a JSON DOM
        /// </summary>
        /// <param name="method"></param>
        /// <param name="type"></param>
        /// <param name="endpoint"></param>
        /// <param name="headers"></param>
        /// <param name="payload"></param>
        /// <returns></returns>
        private async Task<JsonNode> SendRequestAsync(
            HttpMethod method,
            ApiServiceType type,
            string endpoint,
            Dictionary<string, string> headers,
            string payload)
        {
            JsonNode node = null;

            try
            {
                Factory.Logger.LogMessage(Severity.Debug, $"Making request to {endpoint}");

                // Construct a request object, including the headers
                var request = new HttpRequestMessage(method, endpoint);
                foreach (var header in headers)
                {
                    Factory.Logger.LogMessage(Severity.Verbose, $"Adding header {header.Key}: {header.Value}");
                    request.Headers.Add(header.Key, header.Value);
                }

                // If specified, set the body content
                if (!string.IsNullOrEmpty(payload))
                {
                    request.Content = new StringContent(payload, Encoding.UTF8, "application/json");
                }

                // Make a request for the data from the API
                using (var response = await _client.SendAsync(Factory.Logger, type, request))
                {
                    // Check the request was successful
                    if (!response.IsSuccessStatusCode)
                    {
                        Factory.Logger.LogMessage(Severity.Error, $"Response was not successful - code = {response.StatusCode}");
                        return null;
                    }
    
                    // Read the response as a JSON string
                    var json = await response.Content.ReadAsStringAsync();
                    if (string.IsNullOrEmpty(json))
                    {
                        Factory.Logger.LogMessage(Severity.Warning, "Received empty response");
                        return null;
                    }

                    Factory.Logger.LogMessage(Severity.Debug, $"Received response {json}");

                    // Parse the response JSON using lenient options, allowing trailing commas, skipping comments
                    try
                    {
                        var bytes = Encoding.UTF8.GetBytes(json);
                        node = JsonNode.Parse(
                            bytes,
                            nodeOptions: default,
                            documentOptions: new JsonDocumentOptions
                            {
                                AllowTrailingCommas = true,
                                CommentHandling = JsonCommentHandling.Skip
                            }
                        );
                    }
                    catch (Exception ex)
                    {
                        Factory.Logger.LogMessage(Severity.Error, ex.Message);
                        Factory.Logger.LogException(ex);
                        return null;
                    }

                    // It's possible it may have not produced a parsing error but is still not valid. If so,
                    // log the fact
                    if (node == null)
                    {
                        Factory.Logger.LogMessage(Severity.Warning, "JSON response did not parse to a valid JSON node");
                    }
                }
            }
            catch (Exception ex)
            {
                var message = $"Error calling {endpoint}: {ex.Message}";
                Factory.Logger.LogMessage(Severity.Error, message);
                Factory.Logger.LogException(ex);
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
                    Factory.Logger.LogMessage(Severity.Verbose, message);
                }
            }
            else
            {
                // Log the fact that the properties dictionary is NULL
                Factory.Logger.LogMessage(Severity.Warning, "API lookup generated a NULL properties dictionary");
            }
        }

        /// <summary>
        /// Return true if we have a valid value for an API property
        /// </summary>
        /// <param name="properties"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        protected static bool HaveValue(Dictionary<ApiProperty, string> properties, ApiProperty key)
        {
            var value = properties?.ContainsKey(key) == true ? properties[key] : null;
            return !string.IsNullOrEmpty(value);
        }

        /// <summary>
        /// Return a property of a JSON node as an object
        /// </summary>
        /// <param name="node"></param>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        public static object GetObjectValue(JsonNode node, string propertyName)
            => (node is JsonObject jsonObject) && jsonObject.ContainsKey(propertyName) ?
                jsonObject[propertyName] : null;

        /// <summary>
        /// Return a string representation of the value for a given key in a Json object
        /// </summary>
        /// <param name="node"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static string GetStringValue(JsonNode node, string propertyName)
            => (node is JsonObject jsonObject) && jsonObject.ContainsKey(propertyName) ?
                jsonObject[propertyName]?.ToString() ?? "" : "";

        /// <summary>
        /// Extract the value for a given key in a Json object and convert to an interger
        /// </summary>
        /// <param name="node"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static int? GetIntegerValue(JsonNode node, string propertyName)
            => (node is JsonObject jsonObject) && jsonObject.ContainsKey(propertyName) ?
                jsonObject[propertyName]?.GetValue<int?>() : null;
    }
}

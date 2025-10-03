using System.Text.Json.Nodes;
using BaseStationReader.Entities.Logging;
using BaseStationReader.Interfaces.Api;
using BaseStationReader.Interfaces.Logging;

namespace BaseStationReader.BusinessLogic.Api.AirLabs
{
    internal abstract class AirLabsApiBase : ExternalApiBase
    {
        public AirLabsApiBase(ITrackerLogger logger, ITrackerHttpClient client) : base(logger, client)
        {
        }

        /// <summary>
        /// Return a response object list from the API response
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        protected IEnumerable<JsonObject> GetResponseAsObjectList(JsonNode node)
        {
            // Check we have a response
            if (node == null)
            {
                Logger.LogMessage(Severity.Warning, $"API returned NULL");
                return null;
            }

            // Check we have a response array
            var response = node["response"] as JsonArray;
            if (response == null)
            {
                Logger.LogMessage(Severity.Warning, $"API response array is NULL");
                return null;
            }

            // Check the array has some elements
            if (response.Count == 0)
            {
                Logger.LogMessage(Severity.Warning, $"API response array is empty");
                return null;
            }

            // Return elements of the array that are JSON objects
            return response.OfType<JsonObject>();
        }

        /// <summary>
        /// Return the response object from the API response
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        protected JsonObject GetFirstResponseObject(JsonNode node)
        {
            JsonObject responseObject = null;

            // Extract the response array from the response
            var response = GetResponseAsObjectList(node);
            if (response != null)
            {
                // Extract the first element of the response as a JSON object
                responseObject = response.First();
                if (responseObject == null)
                {
                    Logger.LogMessage(Severity.Warning, "API response object is not an JSON object");
                }
            }

            return responseObject;
        }
    }
}
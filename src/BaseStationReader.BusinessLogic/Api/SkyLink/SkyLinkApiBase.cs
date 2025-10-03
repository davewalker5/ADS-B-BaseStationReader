using System.Text.Json.Nodes;
using BaseStationReader.Entities.Logging;
using BaseStationReader.Interfaces.Api;
using BaseStationReader.Interfaces.Logging;

namespace BaseStationReader.BusinessLogic.Api.SkyLink
{
    internal abstract class SkyLinkApiBase : ExternalApiBase
    {
        public SkyLinkApiBase(ITrackerLogger logger, ITrackerHttpClient client) : base(logger, client)
        {
        }

        /// <summary>
        /// Return a response object list from the API response
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        protected IEnumerable<JsonObject> GetResponseObjectList(JsonNode node)
        {
            // Check we have a response
            if (node == null)
            {
                Logger.LogMessage(Severity.Warning, $"API returned NULL");
                return null;
            }

            // Check we have a response array
            var response = node as JsonArray;
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
        protected JsonObject GetResponseObject(JsonNode node)
        {
            JsonObject responseObject = null;

            // Extract the response array from the response
            var response = GetResponseObjectList(node);
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
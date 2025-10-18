using System.Text.Json.Nodes;
using BaseStationReader.Entities.Logging;
using BaseStationReader.Interfaces.Api;
using BaseStationReader.Interfaces.Database;

namespace BaseStationReader.BusinessLogic.Api.SkyLink
{
    internal abstract class SkyLinkApiBase : ExternalApiBase
    {
        public SkyLinkApiBase(
            ITrackerHttpClient client,
            IDatabaseManagementFactory factory) : base(client, factory)
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
                Factory.Logger.LogMessage(Severity.Warning, $"API returned NULL");
                return null;
            }

            // Check we have a response array
            var response = node as JsonArray;
            if (response == null)
            {
                Factory.Logger.LogMessage(Severity.Warning, $"API response array is NULL");
                return null;
            }

            // Check the array has some elements
            if (response.Count == 0)
            {
                Factory.Logger.LogMessage(Severity.Warning, $"API response array is empty");
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
                    Factory.Logger.LogMessage(Severity.Warning, "API response object is not an JSON object");
                }
            }

            return responseObject;
        }

        /// <summary>
        /// Return a response object from the API response
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        protected JsonObject GetResponseAsObject(JsonNode node)
        {
            // Check we have a response
            if (node == null)
            {
                Factory.Logger.LogMessage(Severity.Warning, $"API returned NULL");
                return null;
            }

            // Check we have a response array
            var response = node as JsonObject;
            if (response == null)
            {
                Factory.Logger.LogMessage(Severity.Warning, $"API response object is NULL");
                return null;
            }

            // Return elements of the array that are JSON objects
            return response;
        }
    }
}
using BaseStationReader.BusinessLogic.Api.AeroDatabox;
using BaseStationReader.BusinessLogic.Api.AirLabs;
using BaseStationReader.Data;
using BaseStationReader.Entities.Config;
using BaseStationReader.Interfaces.Logging;
using BaseStationReader.Entities.Logging;
using BaseStationReader.Interfaces.Api;

namespace BaseStationReader.BusinessLogic.Api
{
    public static class ApiWrapperBuilder
    {
        /// <summary>
        /// Build a dictionary where the key is the string representation of the service type member and
        /// the value is the service type
        /// </summary>
        private static readonly Dictionary<string, ApiServiceType> _lookup =
            Enum.GetValues<ApiServiceType>().ToDictionary(e => e.ToString(), e => e);

        /// <summary>
        /// Get an instance of an API wrapper given the required service type
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="settings"></param>
        /// <param name="context"></param>
        /// <param name="client"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static IApiWrapper GetInstance(
            ITrackerLogger logger,
            ExternalApiSettings settings,
            BaseStationReaderDbContext context,
            ITrackerHttpClient client,
            ApiServiceType type)
        {
            IApiWrapper wrapper = type switch
            {
                ApiServiceType.AirLabs => new AirLabsApiWrapper(),
                ApiServiceType.AeroDataBox => new AeroDataBoxApiWrapper(),
                _ => null,
            };

            var valid = wrapper?.Initialise(logger, client, context, settings);
            if (valid != true)
            {
                logger.LogMessage(Severity.Warning, $"API type {type} not configured or unsupported");
                wrapper = null;
            }

            return wrapper;
        }

        /// <summary>
        /// Get an instance of an API wrapper given a string representation of the required service type
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static IApiWrapper GetInstance(
            ITrackerLogger logger,
            ExternalApiSettings settings,
            BaseStationReaderDbContext context,
            ITrackerHttpClient client,
            string type)
            => GetInstance(logger, settings, context, client, GetServiceTypeFromString(type));

        /// <summary>
        /// Return an API service type given a string representation of a service type that may or may
        /// not be valid/supported
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static ApiServiceType GetServiceTypeFromString(string type)
            => !string.IsNullOrEmpty(type) && _lookup.ContainsKey(type) ? _lookup[type] : ApiServiceType.None;
    }
}
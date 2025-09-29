using BaseStationReader.BusinessLogic.Api.AeroDatabox;
using BaseStationReader.BusinessLogic.Api.AirLabs;
using BaseStationReader.Data;
using BaseStationReader.Entities.Config;
using BaseStationReader.Entities.Interfaces;
using BaseStationReader.Entities.Logging;
using BaseStationReader.Entities.Lookup;

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
        /// <param name="type"></param>
        /// <returns></returns>
        public static IApiWrapper GetInstance(ApiServiceType type)
        {
            return type switch
            {
                ApiServiceType.AirLabs => new AirLabsApiWrapper(),
                ApiServiceType.AeroDataBox => new AeroDataBoxApiWrapper(),
                _ => null,
            };
        }

        /// <summary>
        /// Get an instance of an API wrapper given a string representation of the required service type
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static IApiWrapper GetInstance(string type)
            => GetInstance(GetServiceTypeFromString(type));

        /// <summary>
        /// Return an API service type given a string representation of a service type that may or may
        /// not be valid/supported
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static ApiServiceType GetServiceTypeFromString(string type)
            => !string.IsNullOrEmpty(type) && _lookup.ContainsKey(type) ? _lookup[type] : ApiServiceType.None;

        /// <summary>
        /// Construct an API configuration object from a set of API settings
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="context"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static ApiConfiguration BuildApiConfiguration(
            ExternalApiSettings settings,
            BaseStationReaderDbContext context,
            ApiServiceType type)
            => new()
            {
                DatabaseContext = context,
                AirlinesEndpointUrl = settings.ApiEndpoints.FirstOrDefault(x =>
                    x.EndpointType == ApiEndpointType.Airlines && x.Service == type).Url,
                AircraftEndpointUrl = settings.ApiEndpoints.FirstOrDefault(x => x.EndpointType == ApiEndpointType.Aircraft &&
                    x.Service == type).Url,
                FlightsEndpointUrl = settings.ApiEndpoints.FirstOrDefault(x => x.EndpointType == ApiEndpointType.ActiveFlights &&
                    x.Service == type).Url,
                Key = settings.ApiServiceKeys.First(x => x.Service == type).Key
            };

        /// <summary>
        /// Construct an API configuration object from a set of API settings
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="context"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static ApiConfiguration BuildApiConfiguration(
            ExternalApiSettings settings,
            BaseStationReaderDbContext context,
            string type)
            => BuildApiConfiguration(settings, context, GetServiceTypeFromString(type));

        /// <summary>
        /// Configure and return an API wrapper
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public static IApiWrapper ConfigureApiWrapper(
            ITrackerLogger logger,
            ExternalApiSettings settings,
            BaseStationReaderDbContext context,
            ITrackerHttpClient client,
            ApiServiceType type)
        {
            // Build a configuration object
            var config = BuildApiConfiguration(settings, context, type);

            // Configure the API wrapper, if the configuration is valid
            IApiWrapper apiWrapper = null;
            if (config.IsValid)
            {
                apiWrapper = ApiWrapperBuilder.GetInstance(type);
                apiWrapper.Initialise(logger, client, config);
            }
            else
            {
                logger.LogMessage(Severity.Warning, $"API type {type} not configured or unsupported");
            }

            return apiWrapper;
        }

        /// <summary>
        /// Configure and return API wrapper
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="settings"></param>
        /// <param name="context"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static IApiWrapper ConfigureApiWrapper(
            ITrackerLogger logger,
            ExternalApiSettings settings,
            BaseStationReaderDbContext context,
            ITrackerHttpClient client,
            string type)
            => ConfigureApiWrapper(logger, settings, context, client, GetServiceTypeFromString(type));
    }
}
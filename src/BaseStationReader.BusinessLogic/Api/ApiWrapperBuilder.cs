using BaseStationReader.BusinessLogic.Api.AirLabs;
using BaseStationReader.Entities.Config;
using BaseStationReader.Entities.Interfaces;

namespace BaseStationReader.BusinessLogic.Api
{
    public static class ApiWrapperBuilder
    {
        /// <summary>
        /// Build a dictionary where the key is the string representation of the service type member and
        /// the value is the service type
        /// </summary>
        private static Dictionary<string, ApiServiceType> _lookup =
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
    }
}
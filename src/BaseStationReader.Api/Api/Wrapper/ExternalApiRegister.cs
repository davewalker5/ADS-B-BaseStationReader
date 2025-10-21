using System.Collections.Concurrent;
using BaseStationReader.Entities.Config;
using BaseStationReader.Entities.Logging;
using BaseStationReader.Interfaces.Api;
using BaseStationReader.Interfaces.Logging;

namespace BaseStationReader.Api.Wrapper
{
    internal class ExternalApiRegister : IExternalApiRegister
    {
        private readonly ConcurrentDictionary<ApiEndpointType, IExternalApi> _apis = new();
        private readonly ITrackerLogger _logger;

        public ExternalApiRegister(ITrackerLogger logger)
            => _logger = logger;

        /// <summary>
        /// Register an external API instance
        /// </summary>
        /// <param name="type"></param>
        /// <param name="api"></param>
        public void RegisterExternalApi(ApiEndpointType type, IExternalApi api)
            => _apis[type] = api;

        /// <summary>
        /// Retrieve an API instance from the collection
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public IExternalApi GetInstance(ApiEndpointType type)
        {
            if (_apis.TryGetValue(type, out var api))
            {
                _logger.LogMessage(Severity.Debug, $"{type} API is of type {api.GetType().Name}");
            }
            else
            {
                _logger.LogMessage(Severity.Error, $"{type} API not registered");
            }

            return api;
        }
    }
}
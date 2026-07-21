using BaseStationReader.BusinessLogic.Configuration;
using BaseStationReader.Entities.Config;
using BaseStationReader.Interfaces.Logging;
using BaseStationReader.Entities.Logging;
using BaseStationReader.Interfaces.Database;
using BaseStationReader.Interfaces.Api;
using BaseStationReader.Api;

namespace BaseStationReader.Lookup.Logic
{
    internal abstract class LookupHandlerBase : CommandHandlerBase
    {
        protected IExternalApiFactory ApiFactory { get; private set; }

        public LookupHandlerBase(
            LookupToolApplicationSettings settings,
            LookupToolCommandLineParser parser,
            ITrackerLogger logger,
            IDatabaseManagementFactory factory,
            IExternalApiFactory apiFactory) : base(settings, parser, logger, factory)
        {
            ApiFactory = apiFactory;
        }

        /// <summary>
        /// Return an instance of the service
        /// </summary>
        /// <param name="serviceType"></param>
        /// <param name="flightsEndpointType"></param>
        /// <param name="ignoreTrackingStatus"></param>
        /// <returns></returns>
        protected IExternalApiWrapper GetWrapperInstance(string serviceTypeName)
        {
            var serviceType = ApiFactory.GetServiceTypeFromString(serviceTypeName);
            Logger.LogMessage(Severity.Info, $"Using the {serviceType} API");
            return ApiFactory.GetWrapperInstance(TrackerHttpClient.Instance, Factory, serviceType, Settings);

        }
    }
}

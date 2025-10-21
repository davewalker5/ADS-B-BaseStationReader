using BaseStationReader.Entities.Config;
using BaseStationReader.Interfaces.Database;
using BaseStationReader.Interfaces.Logging;

namespace BaseStationReader.Interfaces.Api
{
    public interface IExternalApiFactory
    {
        IExternalApiWrapper GetWrapperInstance(
            ITrackerLogger logger,
            ITrackerHttpClient client,
            IDatabaseManagementFactory factory,
            ApiServiceType service,
            ApiEndpointType flightsEndpointType,
            ExternalApiSettings settings,
            bool ignoreTrackingStatus);

        IExternalApi GetApiInstance(
                    ApiServiceType service,
                    ApiEndpointType endpoint,
                    ITrackerLogger logger,
                    ITrackerHttpClient client,
                    IDatabaseManagementFactory factory,
                    ExternalApiSettings settings);

        ApiServiceType GetServiceTypeFromString(string type);
    }
}
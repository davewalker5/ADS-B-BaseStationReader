using BaseStationReader.Entities.Config;
using BaseStationReader.Interfaces.Database;

namespace BaseStationReader.Interfaces.Api
{
    public interface IExternalApiFactory
    {
        IExternalApiWrapper GetWrapperInstance(
            ITrackerHttpClient client,
            IDatabaseManagementFactory factory,
            ApiServiceType service,
            ExternalApiSettings settings);

        IExternalApi GetApiInstance(
                    ApiServiceType service,
                    ApiEndpointType endpoint,
                    ITrackerHttpClient client,
                    IDatabaseManagementFactory factory,
                    ExternalApiSettings settings);

        ApiServiceType GetServiceTypeFromString(string type);
    }
}
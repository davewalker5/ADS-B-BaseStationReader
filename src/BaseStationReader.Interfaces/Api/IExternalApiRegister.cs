using BaseStationReader.Entities.Config;

namespace BaseStationReader.Interfaces.Api
{
    public interface IExternalApiRegister
    {
        void RegisterExternalApi(ApiEndpointType type, IExternalApi api);
        IExternalApi GetInstance(ApiEndpointType type);
    }
}
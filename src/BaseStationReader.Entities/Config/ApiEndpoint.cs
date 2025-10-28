using System.Diagnostics.CodeAnalysis;

namespace BaseStationReader.Entities.Config
{
    [ExcludeFromCodeCoverage]
    public class ApiEndpoint
    {
        public ApiEndpointType EndpointType { get; set; }
        public string Url { get; set; } = "";
        public string ToString(ApiServiceType service)
            => $"{EndpointType} : {Url}";
    }
}

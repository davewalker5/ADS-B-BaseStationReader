using System.Diagnostics.CodeAnalysis;

namespace BaseStationReader.Entities.Config
{
    [ExcludeFromCodeCoverage]
    public class ApiEndpoint
    {
        public ApiEndpointType EndpointType { get; set; }
        public ApiServiceType Service { get; set; }
        public string Url { get; set; } = "";
        public override string ToString()
            => $"{Service} API : {EndpointType} : {Url}";
    }
}

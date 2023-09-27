using System.Diagnostics.CodeAnalysis;

namespace BaseStationReader.Entities.Config
{
    [ExcludeFromCodeCoverage]
    public class ApiEndpoint
    {
        public ApiEndpointType Type { get; set; }
        public ApiServiceType Service { get; set; }
        public string Url { get; set; } = "";
    }
}

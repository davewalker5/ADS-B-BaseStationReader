using System.Diagnostics.CodeAnalysis;

namespace BaseStationReader.Entities.Config
{
    [ExcludeFromCodeCoverage]
    public class ExternalApiSettings
    {
        public List<ApiEndpoint> ApiEndpoints { get; set; } = [];
        public List<ApiServiceKey> ApiServiceKeys { get; set; } = [];
    }
}
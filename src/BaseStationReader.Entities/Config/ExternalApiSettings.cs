using System.Diagnostics.CodeAnalysis;

namespace BaseStationReader.Entities.Config
{
    [ExcludeFromCodeCoverage]
    public class ExternalApiSettings
    {
        public int MaximumLookups { get; set; }
        public List<ApiEndpoint> ApiEndpoints { get; set; } = [];
        public List<ApiService> ApiServices { get; set; } = [];
    }
}
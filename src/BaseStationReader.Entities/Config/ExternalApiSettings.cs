using System.Diagnostics.CodeAnalysis;

namespace BaseStationReader.Entities.Config
{
    [ExcludeFromCodeCoverage]
    public class ExternalApiSettings
    {
        public List<ApiService> ApiServices { get; set; } = [];
    }
}

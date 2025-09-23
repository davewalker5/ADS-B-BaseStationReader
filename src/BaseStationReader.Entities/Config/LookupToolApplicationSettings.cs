using BaseStationReader.Entities.Logging;
using System.Diagnostics.CodeAnalysis;

namespace BaseStationReader.Entities.Config
{
    [ExcludeFromCodeCoverage]
    public class LookupToolApplicationSettings
    {
        public Severity MinimumLogLevel { get; set; }
        public string LogFile { get; set; } = "";
        public bool CreateSightings { get; set; } = false;
        public List<ApiEndpoint> ApiEndpoints { get; set; } = [];
        public List<ApiServiceKey> ApiServiceKeys { get; set; } = [];
    }
}
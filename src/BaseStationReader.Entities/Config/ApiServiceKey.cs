using System.Diagnostics.CodeAnalysis;

namespace BaseStationReader.Entities.Config
{
    [ExcludeFromCodeCoverage]
    public class ApiServiceKey
    {
        public ApiServiceType Service { get; set; }
        public string Key { get; set; } = "";
    }
}

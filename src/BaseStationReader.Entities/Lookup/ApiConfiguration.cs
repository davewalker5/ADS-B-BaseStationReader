using System.Diagnostics.CodeAnalysis;

namespace BaseStationReader.Entities.Lookup
{
    [ExcludeFromCodeCoverage]
    public class ApiConfiguration
    {
        public object DatabaseContext { get; set; }
        public string AirlinesEndpointUrl { get; set; } = "";
        public string AircraftEndpointUrl { get; set; } = "";
        public string FlightsEndpointUrl { get; set; } = "";
        public string Key { get; set; } = "";
    }
}
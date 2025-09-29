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

        public bool IsValid
        {
            get
            {
                // Need a minimum of the flights and aircraft APIs to do an effective lookup. For some services,
                // such as AeroDataBox, the airline details are returned with the flight details
                return !string.IsNullOrEmpty(AircraftEndpointUrl) && !string.IsNullOrEmpty(FlightsEndpointUrl);
            }
        }
    }
}
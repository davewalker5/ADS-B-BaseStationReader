using System.Diagnostics.CodeAnalysis;

namespace BaseStationReader.Entities.Tracking
{
    [ExcludeFromCodeCoverage]
    public class ApiLookupRequest
    {
        public string AircraftAddress { get; set; }
        public IEnumerable<string> DepartureAirportCodes { get; set; }
        public IEnumerable<string> ArrivalAirportCodes { get; set; }
        public bool CreateSighting { get; set; }
    }
}
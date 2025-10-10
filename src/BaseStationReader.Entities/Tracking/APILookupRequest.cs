using System.Diagnostics.CodeAnalysis;
using BaseStationReader.Entities.Api;
using BaseStationReader.Entities.Config;

namespace BaseStationReader.Entities.Tracking
{
    [ExcludeFromCodeCoverage]
    public class ApiLookupRequest
    {
        public ApiEndpointType FlightEndpointType { get; set; }
        public string AircraftAddress { get; set; }
        public IEnumerable<string> DepartureAirportCodes { get; set; }
        public IEnumerable<string> ArrivalAirportCodes { get; set; }
        public bool CreateSighting { get; set; }

        public ApiProperty FlightPropertyType { get; set; }
        public string FlightPropertyValue { get; set; }
    }
}
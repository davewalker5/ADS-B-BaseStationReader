using BaseStationReader.Entities.Api;

namespace BaseStationReader.Interfaces.Api
{
    public interface IFlightApiWrapper
    {
        Task<Flight> LookupFlightAsync(
            ApiProperty propertyType,
            string propertyValue,
            string aircraftAddress,
            IEnumerable<string> departureAirportCodes,
            IEnumerable<string> arrivalAirportCodes);
    }
}
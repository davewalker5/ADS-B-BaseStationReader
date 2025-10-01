using BaseStationReader.Entities.Api;

namespace BaseStationReader.Interfaces.Api
{
    public interface IFlightApiWrapper
    {
        Task<Flight> LookupFlightAsync(string address, IEnumerable<string> departureAirportCodes, IEnumerable<string> arrivalAirportCodes);
    }
}
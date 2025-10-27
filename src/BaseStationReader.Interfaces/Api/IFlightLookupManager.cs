using BaseStationReader.Entities.Api;

namespace BaseStationReader.Interfaces.Api
{
    public interface IFlightLookupManager
    {
        Task<Flight> IdentifyFlightAsync(string address, IEnumerable<string> departureAirportCodes, IEnumerable<string> arrivalAirportCodes);
    }
}
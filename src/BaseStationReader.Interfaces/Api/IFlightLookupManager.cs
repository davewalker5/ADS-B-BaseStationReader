using BaseStationReader.Entities.Api;
using BaseStationReader.Entities.Tracking;

namespace BaseStationReader.Interfaces.Api
{
    public interface IFlightLookupManager
    {
        Task<Flight> IdentifyFlightAsync(TrackedAircraft aircraft, IEnumerable<string> departureAirportCodes, IEnumerable<string> arrivalAirportCodes);
    }
}
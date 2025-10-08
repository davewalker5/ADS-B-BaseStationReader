using BaseStationReader.Entities.Api;
using BaseStationReader.Entities.Tracking;

namespace BaseStationReader.Interfaces.Api
{
    public interface IFlightNumberApiWrapper
    {
        Task<FlightNumber> GetFlightNumberFromCallsignAsync(string callsign, DateTime? timestamp = null);
        Task<List<FlightNumber>> GetFlightNumbersFromCallsigns(IEnumerable<string> callsigns, DateTime? timestamp = null);
        Task<List<FlightNumber>> GetFlightNumbersForTrackedAircraftAsync(IEnumerable<TrackingStatus> statuses);
    }
}
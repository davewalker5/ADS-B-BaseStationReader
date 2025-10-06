using BaseStationReader.Entities.Heuristics;

namespace BaseStationReader.Interfaces.Database
{
    public interface IConfirmedMappingManager
    {
        Task Truncate();
        Task<ConfirmedMapping> AddAsync(string airlineICAO, string airlineIATA, string flightIATA, string callsign, string digits);
    }
}
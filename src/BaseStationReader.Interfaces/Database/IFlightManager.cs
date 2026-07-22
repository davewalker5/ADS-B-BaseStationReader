using System.Linq.Expressions;
using BaseStationReader.Entities.Api;

namespace BaseStationReader.Interfaces.Database
{
    public interface IFlightManager
    {
        Task<Flight> GetAsync(Expression<Func<Flight, bool>> predicate);
        Task<List<Flight>> ListAsync(Expression<Func<Flight, bool>> predicate);

        Task<Flight> AddAsync(
            string iata,
            string icao,
            string callsign,
            int airlineId,
            int originAirportId,
            int destinationAirportId,
            int provenanceId = 0,
            string embarkation = "",
            string destination = "");
    }
}

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
            string number,
            string embarkation,
            string destination,
            int airlineId);
    }
}
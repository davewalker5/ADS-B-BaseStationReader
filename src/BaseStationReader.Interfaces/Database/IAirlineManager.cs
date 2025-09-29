using BaseStationReader.Entities.Lookup;
using System.Linq.Expressions;

namespace BaseStationReader.Interfaces.Database
{
    public interface IAirlineManager
    {
        Task<Airline> AddAsync(string iata, string icao, string name);
        Task<Airline> GetByCodeAsync(string iata, string icao);
        Task<Airline> GetAsync(Expression<Func<Airline, bool>> predicate);
        Task<List<Airline>> ListAsync(Expression<Func<Airline, bool>> predicate);
    }
}
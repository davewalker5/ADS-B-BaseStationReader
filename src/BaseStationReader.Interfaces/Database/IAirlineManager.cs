using BaseStationReader.Entities.Api;
using System.Linq.Expressions;

namespace BaseStationReader.Interfaces.Database
{
    public interface IAirlineManager
    {
        Task<Airline> AddAsync(string iata, string icao, string name, int provenanceId = 0);
        Task<Airline> UpdateAsync(int id, string iata, string icao, string name, int provenanceId);
        Task DeleteAsync(int id);
        Task<Airline> GetAsync(string iata, string icao, string name);
        Task<Airline> GetAsync(Expression<Func<Airline, bool>> predicate);
        Task<List<Airline>> ListAsync(Expression<Func<Airline, bool>> predicate);
    }
}

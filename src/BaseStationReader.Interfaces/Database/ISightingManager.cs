using System.Linq.Expressions;
using BaseStationReader.Entities.Api;

namespace BaseStationReader.Interfaces.Database
{
    public interface ISightingManager
    {
        Task<Sighting> GetAsync(Expression<Func<Sighting, bool>> predicate);
        Task<List<Sighting>> ListAsync(Expression<Func<Sighting, bool>> predicate);
        Task<Sighting> AddAsync(int aircraftId, int flightId, DateTime timestamp);
    }
}
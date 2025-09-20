using BaseStationReader.Entities.Lookup;
using System.Linq.Expressions;

namespace BaseStationReader.Entities.Interfaces
{
    public interface IAircraftManager
    {
        Task<Aircraft> AddAsync(string address, string registration, int modelId);
        Task<Aircraft> GetAsync(Expression<Func<Aircraft, bool>> predicate);
        Task<List<Aircraft>> ListAsync(Expression<Func<Aircraft, bool>> predicate);
    }
}
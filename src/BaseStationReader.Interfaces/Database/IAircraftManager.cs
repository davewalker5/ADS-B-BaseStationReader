using BaseStationReader.Entities.Api;
using System.Linq.Expressions;

namespace BaseStationReader.Interfaces.Database
{
    public interface IAircraftManager
    {
        Task<Aircraft> AddAsync(string address, string registration, int? manufactured, int? age, int modelId);
        Task<Aircraft> GetAsync(Expression<Func<Aircraft, bool>> predicate);
        Task<List<Aircraft>> ListAsync(Expression<Func<Aircraft, bool>> predicate);
    }
}
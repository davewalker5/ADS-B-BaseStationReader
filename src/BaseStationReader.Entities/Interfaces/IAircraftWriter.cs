using BaseStationReader.Entities.Tracking;
using System.Linq.Expressions;

namespace BaseStationReader.Entities.Interfaces
{
    public interface IAircraftWriter
    {
        Task<Aircraft> GetAsync(Expression<Func<Aircraft, bool>> predicate);
        Task<List<Aircraft>> ListAsync(Expression<Func<Aircraft, bool>> predicate);
        Task<Aircraft> WriteAsync(Aircraft template);
    }
}

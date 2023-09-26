using BaseStationReader.Entities.Lookup;
using System.Linq.Expressions;

namespace BaseStationReader.Entities.Interfaces
{
    public interface IAircraftModelReader
    {
        Task<AircraftModel> GetAsync(Expression<Func<AircraftModel, bool>> predicate);
        Task<List<AircraftModel>> ListAsync(Expression<Func<AircraftModel, bool>> predicate);
    }
}
using BaseStationReader.Entities.Lookup;
using System.Linq.Expressions;

namespace BaseStationReader.Entities.Interfaces
{
    public interface IAircraftDetailsManager
    {
        Task<AircraftDetails> AddAsync(string address, int? airlineId, int? modelId);
        Task<AircraftDetails> GetAsync(Expression<Func<AircraftDetails, bool>> predicate);
        Task<List<AircraftDetails>> ListAsync(Expression<Func<AircraftDetails, bool>> predicate);
    }
}
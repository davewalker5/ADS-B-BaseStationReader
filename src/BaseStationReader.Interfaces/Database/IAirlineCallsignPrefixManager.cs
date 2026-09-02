using BaseStationReader.Entities.Api;
using System.Linq.Expressions;

namespace BaseStationReader.Interfaces.Database
{
    public interface IAirlineCallsignPrefixManager
    {
        Task<AirlineCallsignPrefix> AddAsync(string prefix, int airlineId, int provenanceId = 0);
        Task<AirlineCallsignPrefix> UpdateAsync(int id, string prefix, int airlineId, int provenanceId);
        Task DeleteAsync(int id);
        Task<AirlineCallsignPrefix> GetAsync(Expression<Func<AirlineCallsignPrefix, bool>> predicate);
        Task<List<AirlineCallsignPrefix>> ListAsync(Expression<Func<AirlineCallsignPrefix, bool>> predicate);
    }
}

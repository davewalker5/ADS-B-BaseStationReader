using System.Linq.Expressions;
using BaseStationReader.Entities.Api;

namespace BaseStationReader.Interfaces.Database
{
    public interface IExcludedCallsignManager
    {
        Task<bool> IsExcludedAsync(string callsign);
        Task<List<ExcludedCallsign>> ListAsync(Expression<Func<ExcludedCallsign, bool>> predicate);
        Task<ExcludedCallsign> AddAsync(string callsign);
        Task DeleteAsync(string callsign);
    }
}
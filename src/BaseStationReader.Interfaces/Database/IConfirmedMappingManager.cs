using System.Linq.Expressions;
using BaseStationReader.Entities.Heuristics;

namespace BaseStationReader.Interfaces.Database
{
    public interface IConfirmedMappingManager
    {
        Task<ConfirmedMapping> GetAsync(Expression<Func<ConfirmedMapping, bool>> predicate);
        Task<List<ConfirmedMapping>> ListAsync(Expression<Func<ConfirmedMapping, bool>> predicate);
        Task Truncate();
        Task<ConfirmedMapping> AddAsync(string airlineICAO, string airlineIATA, string flightIATA, string callsign, string digits);
    }
}
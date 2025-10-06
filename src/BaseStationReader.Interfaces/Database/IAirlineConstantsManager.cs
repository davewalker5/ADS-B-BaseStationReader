using System.Linq.Expressions;
using BaseStationReader.Entities.Heuristics;

namespace BaseStationReader.Interfaces.Database
{
    public interface IAirlineConstantsManager
    {
        Task<AirlineConstants> GetAsync(Expression<Func<AirlineConstants, bool>> predicate);
        Task<List<AirlineConstants>> ListAsync(Expression<Func<AirlineConstants, bool>> predicate);
        Task Truncate();
        Task<AirlineConstants> AddAsync(
            string airlineICAO,
            string airlineIATA,
            int? delta,
            decimal purity,
            string prefix,
            decimal identityRate);
    }
}
using System.Linq.Expressions;
using BaseStationReader.Entities.Heuristics;

namespace BaseStationReader.Interfaces.Database
{
    public interface ISuffixDeltaRuleManager
    {
        Task<SuffixDeltaRule> GetAsync(Expression<Func<SuffixDeltaRule, bool>> predicate);
        Task<List<SuffixDeltaRule>> ListAsync(Expression<Func<SuffixDeltaRule, bool>> predicate);
        Task Truncate();
        Task<SuffixDeltaRule> AddAsync(
            string airlineICAO,
            string airlineIATA,
            string suffix,
            int delta,
            int support,
            decimal purity);
    }
}
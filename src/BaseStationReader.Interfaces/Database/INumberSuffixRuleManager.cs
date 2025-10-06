using System.Linq.Expressions;
using BaseStationReader.Entities.Heuristics;

namespace BaseStationReader.Interfaces.Database
{
    public interface INumberSuffixRuleManager
    {
        Task<NumberSuffixRule> GetAsync(Expression<Func<NumberSuffixRule, bool>> predicate);
        Task<List<NumberSuffixRule>> ListAsync(Expression<Func<NumberSuffixRule, bool>> predicate);
        Task Truncate();
        Task<NumberSuffixRule> AddAsync(
            string airlineICAO,
            string airlineIATA,
            string numeric,
            string suffix,
            string digits,
            int support,
            decimal purity);
    }
}
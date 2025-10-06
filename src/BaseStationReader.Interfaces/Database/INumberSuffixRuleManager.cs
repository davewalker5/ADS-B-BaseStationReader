using BaseStationReader.Entities.Heuristics;

namespace BaseStationReader.Interfaces.Database
{
    public interface INumberSuffixRuleManager
    {
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
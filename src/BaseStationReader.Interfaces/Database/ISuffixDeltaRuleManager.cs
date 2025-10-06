using BaseStationReader.Entities.Heuristics;

namespace BaseStationReader.Interfaces.Database
{
    public interface ISuffixDeltaRuleManager
    {
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
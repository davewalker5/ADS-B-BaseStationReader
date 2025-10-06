using BaseStationReader.Entities.Heuristics;
using BaseStationReader.Entities.Import;

namespace BaseStationReader.Interfaces.DataExchange
{
    public interface ISuffixDeltaRuleImporter : ICsvImporter<SuffixDeltaRuleMappingProfile, SuffixDeltaRule>
    {
        Task Truncate();
    }
}
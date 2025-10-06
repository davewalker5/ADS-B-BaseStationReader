using BaseStationReader.Entities.Heuristics;
using BaseStationReader.Entities.Import;

namespace BaseStationReader.Interfaces.DataExchange
{
    public interface INumberSuffixImporter : ICsvImporter<NumberSuffixRuleMappingProfile, NumberSuffixRule>
    {
        Task Truncate();
    }
}
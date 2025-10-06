using BaseStationReader.Entities.Import;
using BaseStationReader.Entities.Messages;

namespace BaseStationReader.Interfaces.DataExchange
{
    public interface INumberSuffixImporter : ICsvImporter<NumberSuffixMappingProfile, NumberSuffix>
    {
        Task Truncate();
    }
}
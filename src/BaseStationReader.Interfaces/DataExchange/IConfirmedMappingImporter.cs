using BaseStationReader.Entities.Heuristics;
using BaseStationReader.Entities.Import;

namespace BaseStationReader.Interfaces.DataExchange
{
    public interface IConfirmedMappingImporter : ICsvImporter<ConfirmedMappingProfile, ConfirmedMapping>
    {
        Task Truncate();
    }
}
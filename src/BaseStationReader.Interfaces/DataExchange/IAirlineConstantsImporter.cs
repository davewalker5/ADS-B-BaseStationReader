using BaseStationReader.Entities.Heuristics;
using BaseStationReader.Entities.Import;

namespace BaseStationReader.Interfaces.DataExchange
{
    public interface IAirlineConstantsImporter : ICsvImporter<AirlineConstantsMappingProfile, AirlineConstants>
    {
        Task Truncate();
    }
}
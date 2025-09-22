using BaseStationReader.Entities.Import;
using BaseStationReader.Entities.Lookup;

namespace BaseStationReader.Entities.Interfaces
{
    public interface IAirlineImporter : ICsvImporter<AirlineMappingProfile, Airline>
    {
    }
}
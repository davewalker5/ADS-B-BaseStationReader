using BaseStationReader.Entities.Import;
using BaseStationReader.Entities.Lookup;

namespace BaseStationReader.Interfaces.DataExchange
{
    public interface IAirlineImporter : ICsvImporter<AirlineMappingProfile, Airline>
    {
    }
}
using BaseStationReader.Entities.Api;
using BaseStationReader.Entities.Import;

namespace BaseStationReader.Interfaces.DataExchange
{
    public interface IAirportImporter : ICsvImporter<AirportMappingProfile, Airport>
    {
    }
}

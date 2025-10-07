using BaseStationReader.Entities.Import;
using BaseStationReader.Entities.Api;

namespace BaseStationReader.Interfaces.DataExchange
{
    public interface IAirlineImporter : ICsvImporter<AirlineMappingProfile, Airline>
    {
    }
}
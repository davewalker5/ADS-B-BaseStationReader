using BaseStationReader.Entities.Api;
using BaseStationReader.Entities.Import;

namespace BaseStationReader.Interfaces.DataExchange
{
    public interface IFlightIATACodeMappingImporter : ICsvImporter<FlightIATACodeMappingProfile, FlightIATACodeMapping>
    {
    }
}
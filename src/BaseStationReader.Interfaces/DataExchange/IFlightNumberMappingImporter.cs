using BaseStationReader.Entities.Api;
using BaseStationReader.Entities.Import;

namespace BaseStationReader.Interfaces.DataExchange
{
    public interface IFlightNumberMappingImporter : ICsvImporter<FlightNumberMappingProfile, FlightNumberMapping>
    {
        Task Truncate();
    }
}
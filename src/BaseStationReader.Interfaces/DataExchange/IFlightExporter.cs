using BaseStationReader.Entities.Api;

namespace BaseStationReader.Interfaces.DataExchange
{
    public interface IFlightExporter
    {
        void Export(IEnumerable<Flight> flights, string file);
    }
}
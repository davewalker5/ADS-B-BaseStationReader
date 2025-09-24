using BaseStationReader.Entities.Lookup;

namespace BaseStationReader.Entities.Interfaces
{
    public interface IFlightExporter
    {
        void Export(IEnumerable<Flight> flights, string file);
    }
}
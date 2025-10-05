using BaseStationReader.Entities.Api;

namespace BaseStationReader.Interfaces.DataExchange
{
    public interface IFlightNumberExporter
    {
        void Export(IEnumerable<FlightNumber> flightNumbers, string file);
    }
}
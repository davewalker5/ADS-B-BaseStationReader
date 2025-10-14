using System.Linq.Expressions;
using BaseStationReader.Entities.Api;

namespace BaseStationReader.Interfaces.Database
{
    public interface IFlightNumberMappingManager
    {
        Task<FlightNumberMapping> GetAsync(Expression<Func<FlightNumberMapping, bool>> predicate);
        Task<List<FlightNumberMapping>> ListAsync(Expression<Func<FlightNumberMapping, bool>> predicate);
        Task<FlightNumberMapping> AddAsync(
            string airlineICAO,
            string airlineIATA,
            string airlineName,
            string airportICAO,
            string airportIATA,
            string airportName,
            AirportType airportType,
            string embarkation,
            string destination,
            string flightIATA,
            string callsign,
            string filename);
    }
}
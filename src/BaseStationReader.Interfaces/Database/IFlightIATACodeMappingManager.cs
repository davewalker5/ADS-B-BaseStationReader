using System.Linq.Expressions;
using BaseStationReader.Entities.Api;

namespace BaseStationReader.Interfaces.Database
{
    public interface IFlightIATACodeMappingManager
    {
        Task<FlightIATACodeMapping> GetAsync(Expression<Func<FlightIATACodeMapping, bool>> predicate);
        Task<List<FlightIATACodeMapping>> ListAsync(Expression<Func<FlightIATACodeMapping, bool>> predicate);
        Task<FlightIATACodeMapping> AddAsync(
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
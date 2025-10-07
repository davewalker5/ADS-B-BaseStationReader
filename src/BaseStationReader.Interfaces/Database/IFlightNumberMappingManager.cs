using System.Linq.Expressions;
using BaseStationReader.Entities.Api;

namespace BaseStationReader.Interfaces.Database
{
    public interface IFlightNumberMappingManager
    {
        Task<FlightNumberMapping> GetAsync(Expression<Func<FlightNumberMapping, bool>> predicate);
        Task<List<FlightNumberMapping>> ListAsync(Expression<Func<FlightNumberMapping, bool>> predicate);
        Task Truncate();
        Task<FlightNumberMapping> AddAsync(string airlineICAO, string airlineIATA, string flightIATA, string callsign);
    }
}
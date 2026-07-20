using BaseStationReader.Entities.Api;
using System.Linq.Expressions;

namespace BaseStationReader.Interfaces.Database
{
    public interface IAirportManager
    {
        /// <summary>
        /// Add an airport if it does not already exist.
        /// </summary>
        Task<Airport> AddAsync(Airport airport);

        /// <summary>
        /// Return an airport by ICAO, IATA or name, in that order.
        /// </summary>
        Task<Airport> GetAsync(string iata, string icao, string name);

        /// <summary>
        /// Return the first airport matching the specified criteria.
        /// </summary>
        Task<Airport> GetAsync(Expression<Func<Airport, bool>> predicate);

        /// <summary>
        /// Return all airports matching the specified criteria.
        /// </summary>
        Task<List<Airport>> ListAsync(Expression<Func<Airport, bool>> predicate);
    }
}

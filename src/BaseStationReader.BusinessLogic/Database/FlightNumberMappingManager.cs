using System.Linq.Expressions;
using BaseStationReader.Data;
using BaseStationReader.Entities.Api;
using BaseStationReader.Interfaces.Database;
using Microsoft.EntityFrameworkCore;

namespace BaseStationReader.BusinessLogic.Database
{
    internal class FlightNumberMappingManager : IFlightNumberMappingManager
    {
        private readonly BaseStationReaderDbContext _context;

        public FlightNumberMappingManager(BaseStationReaderDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Return the first confirmed mapping matching the specified criteria
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public async Task<FlightNumberMapping> GetAsync(Expression<Func<FlightNumberMapping, bool>> predicate)
        {
            List<FlightNumberMapping> mappings = await ListAsync(predicate);
            return mappings.FirstOrDefault();
        }

        /// <summary>
        /// Get a list of confirmed mappings matching the specified criteria
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public async Task<List<FlightNumberMapping>> ListAsync(Expression<Func<FlightNumberMapping, bool>> predicate)
            => await _context.ConfirmedMappings.Where(predicate).ToListAsync();

        /// <summary>
        /// Truncate the confirmed mappings table to remove all existing entries
        /// </summary>
        /// <returns></returns>
        public async Task Truncate()
            => await _context.TruncateConfirmedMappings();
        
        /// <summary>
        /// Add a confirmed mapping between callsign and flight number
        /// </summary>
        /// <param name="airlineICAO"></param>
        /// <param name="airlineIATA"></param>
        /// <param name="flightIATA"></param>
        /// <param name="callsign"></param>
        /// <returns></returns>
        public async Task<FlightNumberMapping> AddAsync(
            string airlineICAO,
            string airlineIATA,
            string flightIATA,
            string callsign)
        {
            var mapping = new FlightNumberMapping()
            {
                AirlineICAO = airlineICAO,
                AirlineIATA = airlineIATA,
                FlightIATA = flightIATA,
                Callsign = callsign
            };

            await _context.ConfirmedMappings.AddAsync(mapping);
            await _context.SaveChangesAsync();
            return mapping;
        }
    }
}
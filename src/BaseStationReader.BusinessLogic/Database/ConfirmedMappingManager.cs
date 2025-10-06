using System.Linq.Expressions;
using BaseStationReader.Data;
using BaseStationReader.Entities.Heuristics;
using BaseStationReader.Interfaces.Database;
using Microsoft.EntityFrameworkCore;

namespace BaseStationReader.BusinessLogic.Database
{
    internal class ConfirmedMappingManager : IConfirmedMappingManager
    {
        private readonly BaseStationReaderDbContext _context;

        public ConfirmedMappingManager(BaseStationReaderDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Return the first confirmed mapping matching the specified criteria
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public async Task<ConfirmedMapping> GetAsync(Expression<Func<ConfirmedMapping, bool>> predicate)
        {
            List<ConfirmedMapping> mappings = await ListAsync(predicate);
            return mappings.FirstOrDefault();
        }

        /// <summary>
        /// Get a list of confirmed mappings matching the specified criteria
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public async Task<List<ConfirmedMapping>> ListAsync(Expression<Func<ConfirmedMapping, bool>> predicate)
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
        /// <param name="digits"></param>
        /// <returns></returns>
        public async Task<ConfirmedMapping> AddAsync(
            string airlineICAO,
            string airlineIATA,
            string flightIATA,
            string callsign,
            string digits)
        {
            var mapping = new ConfirmedMapping()
            {
                AirlineICAO = airlineICAO,
                AirlineIATA = airlineIATA,
                FlightIATA = flightIATA,
                Callsign = callsign,
                Digits = digits
            };

            await _context.ConfirmedMappings.AddAsync(mapping);
            await _context.SaveChangesAsync();
            return mapping;
        }
    }
}
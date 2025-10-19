using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using BaseStationReader.Data;
using BaseStationReader.Entities.Api;
using BaseStationReader.Interfaces.Database;
using Microsoft.EntityFrameworkCore;

namespace BaseStationReader.BusinessLogic.Database
{
    [ExcludeFromCodeCoverage]
    internal class ExcludedCallsignManager : IExcludedCallsignManager
    {
        private readonly BaseStationReaderDbContext _context;

        public ExcludedCallsignManager(BaseStationReaderDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Return true if a callsign is excluded
        /// </summary>
        /// <param name="callsign"></param>
        /// <returns></returns>
        public async Task<bool> IsExcludedAsync(string callsign)
        {
            var exclusions = await ListAsync(x => x.Callsign == callsign);
            return exclusions.Count > 0;
        }

        /// <summary>
        /// List all exclusions matching the specified predicate
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public async Task<List<ExcludedCallsign>> ListAsync(Expression<Func<ExcludedCallsign, bool>> predicate)
            => await _context.ExcludedCallsigns
                .Where(predicate)
                .OrderBy(x => x.Callsign)
                .ToListAsync();

        /// <summary>
        /// Add a flight, if the associated ICAO callsign doesn't already exist
        /// </summary>
        /// <param name="callsign"></param>
        /// <param name="registration"></param>
        /// <param name="manufactured"></param>
        /// <param name="age"></param>
        /// <param name="modelId"></param>
        /// <returns></returns>
        public async Task<ExcludedCallsign> AddAsync(string callsign)
        {
            // Check there's not already an exclusion for this callsign
            var exclusion = await _context.ExcludedCallsigns.FirstOrDefaultAsync(x => x.Callsign == callsign);
            if (exclusion == null)
            {
                // Create a new exclusion
                exclusion = new ExcludedCallsign()
                {
                    Callsign = callsign
                };

                // Save the aircraft
                await _context.ExcludedCallsigns.AddAsync(exclusion);
                await _context.SaveChangesAsync();
            }

            return exclusion;
        }
        
        /// <summary>
        /// Delete the exclusion record for the specified callsign
        /// </summary>
        /// <param name="callsign"></param>
        /// <returns></returns>
        public async Task DeleteAsync(string callsign)
        {
            // Find the exclusion record
            var exclusion = await _context.ExcludedCallsigns.FirstOrDefaultAsync(x => x.Callsign == callsign);
            if (exclusion != null)
            {
                // Found one, so remove it
                _context.Remove(exclusion);
                await _context.SaveChangesAsync();
            }
        }
    }
}
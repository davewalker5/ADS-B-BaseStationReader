using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using BaseStationReader.Data;
using BaseStationReader.Entities.Tracking;
using BaseStationReader.Interfaces.Database;
using Microsoft.EntityFrameworkCore;

namespace BaseStationReader.BusinessLogic.Database
{
    [ExcludeFromCodeCoverage]
    internal class ExcludedAddressManager : IExcludedAddressManager
    {
        private readonly BaseStationReaderDbContext _context;

        public ExcludedAddressManager(BaseStationReaderDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Return true if an aircraft address is excluded
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public async Task<bool> IsExcludedAsync(string address)
        {
            var exclusions = await ListAsync(x => x.Address == address);
            return exclusions.Count > 0;
        }

        /// <summary>
        /// List all exclusions matching the specified predicate
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public async Task<List<ExcludedAddress>> ListAsync(Expression<Func<ExcludedAddress, bool>> predicate)
            => await _context.ExcludedAddresses
                .Where(predicate)
                .OrderBy(x => x.Address)
                .ToListAsync();

        /// <summary>
        /// Add an aircraft, if the associated ICAO address doesn't already exist
        /// </summary>
        /// <param name="address"></param>
        /// <param name="registration"></param>
        /// <param name="manufactured"></param>
        /// <param name="age"></param>
        /// <param name="modelId"></param>
        /// <returns></returns>
        public async Task<ExcludedAddress> AddAsync(string address)
        {
            // Check there's not already an exclusion for this address
            var exclusion = await _context.ExcludedAddresses.FirstOrDefaultAsync(x => x.Address == address);
            if (exclusion == null)
            {
                // Create a new exclusion
                exclusion = new ExcludedAddress()
                {
                    Address = address
                };

                // Save the aircraft
                await _context.ExcludedAddresses.AddAsync(exclusion);
                await _context.SaveChangesAsync();
            }

            return exclusion;
        }
        
        /// <summary>
        /// Delete the exclusion record for the specified address
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public async Task DeleteAsync(string address)
        {
            // Find the exclusion record
            var exclusion = await _context.ExcludedAddresses.FirstOrDefaultAsync(x => x.Address == address);
            if (exclusion != null)
            {
                // Found one, so remove it
                _context.Remove(exclusion);
                await _context.SaveChangesAsync();
            }
        }
    }
}
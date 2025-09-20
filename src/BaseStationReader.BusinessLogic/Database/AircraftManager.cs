using BaseStationReader.Data;
using BaseStationReader.Entities.Interfaces;
using BaseStationReader.Entities.Lookup;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace BaseStationReader.BusinessLogic.Database
{
    public class AircraftManager : IAircraftDetailsManager
    {
        private readonly BaseStationReaderDbContext _context;

        public AircraftManager(BaseStationReaderDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Return the first set of details matching the specified criteria
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public async Task<Aircraft> GetAsync(Expression<Func<Aircraft, bool>> predicate)
        {
            List<Aircraft> details = await ListAsync(predicate);
            return details.FirstOrDefault();
        }

        /// <summary>
        /// Return all details matching the specified criteria
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public async Task<List<Aircraft>> ListAsync(Expression<Func<Aircraft, bool>> predicate)
            => await _context.Aircraft
                .Where(predicate)
                .ToListAsync();

        /// <summary>
        /// Add an aircraft, if the associated ICAO address doesn't already exist
        /// </summary>
        /// <param name="iata"></param>
        /// <param name="icao"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public async Task<Aircraft> AddAsync(string address, string registration, int modelId)
        {
            var details = await GetAsync(a => a.Address == address);

            if (details == null)
            {
                details = new Aircraft
                {
                    Address = address,
                    Registration = registration,
                    ModelId = modelId
                };

                await _context.Aircraft.AddAsync(details);
                await _context.SaveChangesAsync();
            }

            return details;
        }
    }
}

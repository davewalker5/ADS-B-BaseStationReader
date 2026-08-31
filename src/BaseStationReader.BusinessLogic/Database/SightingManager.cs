using BaseStationReader.Data;
using BaseStationReader.Entities.Api;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using BaseStationReader.Interfaces.Database;

namespace BaseStationReader.BusinessLogic.Database
{
    public class SightingManager : ISightingManager
    {
        private readonly BaseStationReaderDbContext _context;

        public SightingManager(BaseStationReaderDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Return the first sighting matching the specified criteria
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public async Task<Sighting> GetAsync(Expression<Func<Sighting, bool>> predicate)
        {
            List<Sighting> sighting = await ListAsync(predicate);
            return sighting.FirstOrDefault();
        }

        /// <summary>
        /// Return all sightings matching the specified criteria
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public async Task<List<Sighting>> ListAsync(Expression<Func<Sighting, bool>> predicate)
            => await _context.Sightings
                .AsNoTracking()
                .Where(predicate)
                .Include(x => x.Aircraft)
                    .ThenInclude(x => x.Model)
                        .ThenInclude(x => x.Manufacturer)
                .Include(x => x.Flight)
                    .ThenInclude(x => x.Airline)
                .Include(x => x.Airline)
                .ToListAsync();

    }
}

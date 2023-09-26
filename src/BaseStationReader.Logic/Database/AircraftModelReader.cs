using BaseStationReader.Data;
using BaseStationReader.Entities.Interfaces;
using BaseStationReader.Entities.Lookup;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace BaseStationReader.Logic.Database
{
    public class AircraftModelReader : IAircraftModelReader
    {
        private readonly BaseStationReaderDbContext _context;

        public AircraftModelReader(BaseStationReaderDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Get the first aircraft model matching the specified criteria
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public async Task<AircraftModel> GetAsync(Expression<Func<AircraftModel, bool>> predicate)
        {
            List<AircraftModel> models = await ListAsync(predicate);

#pragma warning disable CS8603
            return models.FirstOrDefault();
#pragma warning restore CS8603
        }

        /// <summary>
        /// Return all aircraft models matching the specified criteria
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public async Task<List<AircraftModel>> ListAsync(Expression<Func<AircraftModel, bool>> predicate)
            => await _context
                .AircraftModels
                .Where(predicate)
                .Include(x => x.Manufacturer)
                .Include(x => x.WakeTurbulenceCategory)
                .ToListAsync();
    }
}

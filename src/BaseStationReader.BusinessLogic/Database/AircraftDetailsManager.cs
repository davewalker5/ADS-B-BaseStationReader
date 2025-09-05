using BaseStationReader.Data;
using BaseStationReader.Entities.Interfaces;
using BaseStationReader.Entities.Lookup;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace BaseStationReader.BusinessLogic.Database
{
    public class AircraftDetailsManager : IAircraftDetailsManager
    {
        private readonly BaseStationReaderDbContext _context;

        public AircraftDetailsManager(BaseStationReaderDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Return the first set of details matching the specified criteria
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public async Task<AircraftDetails> GetAsync(Expression<Func<AircraftDetails, bool>> predicate)
        {
            List<AircraftDetails> details = await ListAsync(predicate);

#pragma warning disable CS8603
            return details.FirstOrDefault();
#pragma warning restore CS8603
        }

        /// <summary>
        /// Return all details matching the specified criteria
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public async Task<List<AircraftDetails>> ListAsync(Expression<Func<AircraftDetails, bool>> predicate)
#pragma warning disable CS8602
            => await _context.AircraftDetails
                .Where(predicate)
                .Include(a => a.Airline)
                .Include(a => a.Model)
                .ThenInclude(m => m.Manufacturer)
                .ToListAsync();
#pragma warning restore CS8602

        /// <summary>
        /// Add a set of details, if the associated ICAO address doesn't already exist
        /// </summary>
        /// <param name="iata"></param>
        /// <param name="icao"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public async Task<AircraftDetails> AddAsync(string address, int? airlineId, int? modelId)
        {
            var details = await GetAsync(a => a.Address == address);

            if (details == null)
            {
                details = new AircraftDetails { Address = address, AirlineId = airlineId, ModelId = modelId };
                await _context.AircraftDetails.AddAsync(details);
                await _context.SaveChangesAsync();
            }

            return details;
        }
    }
}

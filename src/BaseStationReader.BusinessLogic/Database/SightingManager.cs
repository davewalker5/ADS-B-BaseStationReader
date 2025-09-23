using BaseStationReader.Data;
using BaseStationReader.Entities.Interfaces;
using BaseStationReader.Entities.Lookup;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

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
                .Where(predicate)
                .Include(x => x.Aircraft)
                    .ThenInclude(x => x.Model)
                        .ThenInclude(x => x.Manufacturer)
                .Include(x => x.Flight)
                    .ThenInclude(x => x.Airline)
                .ToListAsync();

        /// <summary>
        /// Add a sighting
        /// </summary>
        /// <param name="aircraftId"></param>
        /// <param name="flightId"></param>
        /// <param name="timestamp"></param>
        /// <returns></returns>
        public async Task<Sighting> AddAsync(int aircraftId, int flightId, DateTime timestamp)
        {
            // See if there's an existing sighting on this date for this aircraft and flight
            var sighting = await GetAsync(x =>
                (x.AircraftId == aircraftId) &&
                (x.FlightId == flightId) &&
                (x.Timestamp == timestamp));

            if (sighting == null)
            {
                // No existing sighting, so create a new one
                sighting = new Sighting
                {
                    AircraftId = aircraftId,
                    FlightId = flightId,
                    Timestamp = timestamp
                };

                // Save the sighting
                await _context.Sightings.AddAsync(sighting);
                await _context.SaveChangesAsync();

                // Re-load to retrieve the associated entities
                sighting = await GetAsync(x => x.Id == sighting.Id);
            }

            return sighting;
        }
    }
}

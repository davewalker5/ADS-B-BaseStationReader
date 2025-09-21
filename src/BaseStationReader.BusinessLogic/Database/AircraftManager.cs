using BaseStationReader.Data;
using BaseStationReader.Entities.Interfaces;
using BaseStationReader.Entities.Lookup;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace BaseStationReader.BusinessLogic.Database
{
    public class AircraftManager : IAircraftManager
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
                .Include(x => x.Model)
                .ThenInclude(x => x.Manufacturer)
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
        public async Task<Aircraft> AddAsync(string address, string registration, int? manufactured, int? age, int modelId)
        {
            var aircraft = await GetAsync(a => a.Address == address);

            if (aircraft == null)
            {
                // Create a new instance
                aircraft = new Aircraft
                {
                    Address = address,
                    Registration = registration,
                    Manufactured = manufactured,
                    Age = age,
                    ModelId = modelId
                };

                // Save the aircraft
                await _context.Aircraft.AddAsync(aircraft);
                await _context.SaveChangesAsync();

                // Load related entities
                await _context.Entry(aircraft).Reference(x => x.Model).LoadAsync();
                await _context.Entry(aircraft.Model).Reference(x => x.Manufacturer).LoadAsync();
            }

            return aircraft;
        }
    }
}

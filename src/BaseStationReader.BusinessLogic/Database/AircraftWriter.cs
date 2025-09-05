using BaseStationReader.Data;
using BaseStationReader.Entities.Interfaces;
using BaseStationReader.Entities.Tracking;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Reflection;

namespace BaseStationReader.BusinessLogic.Database
{
    public class AircraftWriter : IAircraftWriter
    {
        private readonly BaseStationReaderDbContext _context;
        private readonly PropertyInfo[] _aircraftProperties = typeof(Aircraft)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(x => x.Name != "Id")
            .ToArray();

        public AircraftWriter(BaseStationReaderDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Get the most recent aircraft matching the specified criteria
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public async Task<Aircraft> GetAsync(Expression<Func<Aircraft, bool>> predicate)
        {
            List<Aircraft> aircraft = await ListAsync(predicate);
#pragma warning disable CS8603
            return aircraft.FirstOrDefault();
#pragma warning restore CS8603
        }

        /// <summary>
        /// List all aircraft matching the specified criteria, most recent first
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public async Task<List<Aircraft>> ListAsync(Expression<Func<Aircraft, bool>> predicate)
            => await _context.Aircraft.Where(predicate).OrderByDescending(x => x.LastSeen).ToListAsync();

        /// <summary>
        /// Write an aircraft to the database, either creating a new record or updating an existing one
        /// </summary>
        /// <param name="template"></param>
        /// <returns></returns>
        public async Task<Aircraft> WriteAsync(Aircraft template)
        {
            // If the template has an ID associated with it, retrieve that record for update
            Aircraft aircraft = null;
            if (template.Id > 0)
            {
                aircraft = await GetAsync(x => x.Id == template.Id);
            }

            // If we still don't have an aircraft instance, we're creating a new one
            if (aircraft == null)
            {
                aircraft = new Aircraft();
                await _context.AddAsync(aircraft);
            }

            // Update the aircraft properties
            foreach (var aircraftProperty in _aircraftProperties)
            {
                var updated = aircraftProperty.GetValue(template);
                aircraftProperty.SetValue(aircraft, updated);
            }

            // Save changes
            await _context.SaveChangesAsync();

            return aircraft;
        }
    }
}

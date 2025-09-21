using BaseStationReader.Data;
using BaseStationReader.Entities.Interfaces;
using BaseStationReader.Entities.Tracking;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Reflection;

namespace BaseStationReader.BusinessLogic.Database
{
    public class TrackedAircraftWriter : IAircraftWriter
    {
        private readonly BaseStationReaderDbContext _context;
        private readonly PropertyInfo[] _aircraftProperties = typeof(TrackedAircraft)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(x => x.Name != "Id")
            .ToArray();

        public TrackedAircraftWriter(BaseStationReaderDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Get the most recent aircraft matching the specified criteria
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public async Task<TrackedAircraft> GetAsync(Expression<Func<TrackedAircraft, bool>> predicate)
        {
            List<TrackedAircraft> aircraft = await ListAsync(predicate);
            return aircraft.FirstOrDefault();
        }

        /// <summary>
        /// List all aircraft matching the specified criteria, most recent first
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public async Task<List<TrackedAircraft>> ListAsync(Expression<Func<TrackedAircraft, bool>> predicate)
            => await _context.TrackedAircraft.Where(predicate).OrderByDescending(x => x.LastSeen).ToListAsync();

        /// <summary>
        /// Write an aircraft to the database, either creating a new record or updating an existing one
        /// </summary>
        /// <param name="template"></param>
        /// <returns></returns>
        public async Task<TrackedAircraft> WriteAsync(TrackedAircraft template)
        {
            // If the template has an ID associated with it, retrieve that record for update
            TrackedAircraft aircraft = null;
            if (template.Id > 0)
            {
                aircraft = await GetAsync(x => x.Id == template.Id);
            }

            // If we still don't have an aircraft instance, we're creating a new one
            if (aircraft == null)
            {
                aircraft = new TrackedAircraft();
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

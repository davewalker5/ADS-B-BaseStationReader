using BaseStationReader.Data;
using BaseStationReader.Interfaces.Tracking;
using BaseStationReader.Entities.Tracking;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Reflection;

namespace BaseStationReader.BusinessLogic.Database
{
    public class TrackedAircraftWriter : ITrackedAircraftWriter
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
            // Find existing matching aircraft records
            var aircraft = await _context.TrackedAircraft.FirstOrDefaultAsync(x => x.Id == template.Id);
            if (aircraft != null)
            {
                // The lookup timestamp may be set on the database but not in the incoming template
                // so make sure the value from the database is retained
                template.LookupTimestamp ??= aircraft.LookupTimestamp;

                // Record found, so update its properties
                UpdateProperties(template, aircraft);
            }
            else
            {
                // Existing record not found, so add a new one
                aircraft = new();
                UpdateProperties(template, aircraft);
                await _context.TrackedAircraft.AddAsync(aircraft);
            }

            // Save changes
            await _context.SaveChangesAsync();
            return aircraft;
        }

        /// <summary>
        /// Set the lookup timestamp on a tracked aircraft
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public async Task<TrackedAircraft> SetLookupTimestamp(string address)
        {
            var aircraft = await _context.TrackedAircraft.FirstOrDefaultAsync(x => (x.Address == address) && (x.LookupTimestamp == null));

            if (aircraft != null)
            {
                aircraft.LookupTimestamp = DateTime.Now;
                await _context.SaveChangesAsync();
            }

            return aircraft;
        }

        /// <summary>
        /// Update the properties of a tracked aircraft
        /// </summary>
        /// <param name="source"></param>
        /// <param name="destination"></param>
        private void UpdateProperties(TrackedAircraft source, TrackedAircraft destination)
        {
            foreach (var positionProperty in _aircraftProperties)
            {
                var updated = positionProperty.GetValue(source);
                positionProperty.SetValue(destination, updated);
            }
        }
    }
}

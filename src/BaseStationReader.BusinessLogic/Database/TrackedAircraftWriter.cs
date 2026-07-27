using BaseStationReader.Data;
using BaseStationReader.Interfaces.Tracking;
using BaseStationReader.Entities.Tracking;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Reflection;

namespace BaseStationReader.BusinessLogic.Database
{
    internal class TrackedAircraftWriter : ITrackedAircraftWriter
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
        /// Get the most recently seen aircraft matching the specified criteria
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public async Task<TrackedAircraft> GetAsync(Expression<Func<TrackedAircraft, bool>> predicate)
        {
            var aircraft = await ListAsync(predicate);
            return aircraft.FirstOrDefault();
        }

        /// <summary>
        /// List all aircraft matching the specified criteria, most recent first
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public async Task<List<TrackedAircraft>> ListAsync(Expression<Func<TrackedAircraft, bool>> predicate)
            => await _context.TrackedAircraft
                             .Where(predicate)
                             .OrderByDescending(x => x.LastSeen)
                             .ToListAsync();

        /// <summary>
        /// Write an aircraft to the database, either creating a new record or updating an existing one
        /// </summary>
        /// <param name="template"></param>
        /// <returns></returns>
        public async Task<TrackedAircraft> WriteAsync(TrackedAircraft template)
        {
            // Find an existing matching tracked aircraft record. If the ID isn't set, look for a match
            // by address for an aircraft that's still active. This logic is to prevent multiple
            // duplicate aircraft records being created if a flurry of messages come in when an aircraft
            // is first tracked
            var aircraft = template.Id > 0 ?
                    await _context.TrackedAircraft.FirstOrDefaultAsync(x => x.Id == template.Id) :
                    await _context.TrackedAircraft.FirstOrDefaultAsync(x =>
                        (x.Address == template.Address) &&
                        (x.Status == TrackingStatus.Active) &&
                        (x.SessionId == template.SessionId));

            if (aircraft != null)
            {
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

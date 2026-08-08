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
        private readonly PropertyInfo[] _aircraftProperties;

        public TrackedAircraftWriter(BaseStationReaderDbContext context)
        {
            _context = context;
            // EF's mapped-property metadata contains scalar columns only. In particular, it excludes the
            // Session navigation property, whose null value would otherwise clear SessionId during updates.
            _aircraftProperties = context.Model.FindEntityType(typeof(TrackedAircraft))!
                .GetProperties()
                .Where(property => property.Name != nameof(TrackedAircraft.Id))
                .Select(property => property.PropertyInfo)
                .Where(property => property is not null)
                .Cast<PropertyInfo>()
                .ToArray();
        }

        /// <summary>
        /// Get the most recently seen aircraft matching the specified criteria
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public async Task<TrackedAircraft> GetAsync(Expression<Func<TrackedAircraft, bool>> predicate, CancellationToken cancellationToken = default)
        {
            var aircraft = await _context.TrackedAircraft
                .Where(predicate)
                .OrderByDescending(x => x.LastSeen)
                .ToListAsync(cancellationToken);
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
        public async Task<TrackedAircraft> WriteAsync(TrackedAircraft template, CancellationToken cancellationToken = default)
        {
            // Lifetime resolution is owned by AircraftLifetimeManager. An unset ID therefore always starts
            // a new lifetime instead of introducing a second, status-based continuity rule here.
            var aircraft = template.Id > 0
                ? await _context.TrackedAircraft.FirstOrDefaultAsync(x => x.Id == template.Id, cancellationToken)
                : null;

            if (aircraft != null)
            {
                // Record found, so update its properties
                MergeProperties(template, aircraft);
            }
            else
            {
                // Existing record not found, so add a new one
                aircraft = new();
                UpdateProperties(template, aircraft);
                await _context.TrackedAircraft.AddAsync(aircraft, cancellationToken);
            }

            // Save changes
            await _context.SaveChangesAsync(cancellationToken);
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

        /// <summary>
        /// Merges an observation into an existing lifetime without regressing its boundaries or message count.
        /// </summary>
        /// <param name="source">Queued aircraft observation.</param>
        /// <param name="destination">Persisted aircraft lifetime.</param>
        private void MergeProperties(TrackedAircraft source, TrackedAircraft destination)
        {
            var firstSeen = source.FirstSeen < destination.FirstSeen ? source.FirstSeen : destination.FirstSeen;
            var lastSeen = source.LastSeen > destination.LastSeen ? source.LastSeen : destination.LastSeen;
            var messages = Math.Max(source.Messages, destination.Messages);

            // Equal timestamps can carry a later status transition. Older out-of-order snapshots must not
            // replace the latest aircraft properties or move the persisted lifetime backwards.
            if (source.LastSeen >= destination.LastSeen)
            {
                UpdateProperties(source, destination);
            }

            destination.FirstSeen = firstSeen;
            destination.LastSeen = lastSeen;
            destination.Messages = messages;
        }

    }
}

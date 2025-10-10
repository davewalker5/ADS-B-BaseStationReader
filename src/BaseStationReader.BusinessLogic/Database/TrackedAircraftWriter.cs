using BaseStationReader.Data;
using BaseStationReader.Interfaces.Tracking;
using BaseStationReader.Entities.Tracking;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Reflection;
using BaseStationReader.Interfaces.Logging;
using BaseStationReader.Entities.Logging;

namespace BaseStationReader.BusinessLogic.Database
{
    internal class TrackedAircraftWriter : ITrackedAircraftWriter
    {
        private readonly int _maximumLookups;
        private readonly ITrackerLogger _logger;
        private readonly BaseStationReaderDbContext _context;
        private readonly PropertyInfo[] _aircraftProperties = typeof(TrackedAircraft)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(x => x.Name != "Id")
            .ToArray();

        public TrackedAircraftWriter(ITrackerLogger logger, BaseStationReaderDbContext context, int maximumLookups)
        {
            _logger = logger;
            _context = context;
            _maximumLookups = maximumLookups;
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
        /// Return the lookup candidate with the specified address
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public async Task<TrackedAircraft> GetLookupCandidateAsync(string address)
        {
            var candidates = await ListLookupCandidatesAsync();
            var aircraft = candidates.FirstOrDefault(x => x.Address == address);
            return aircraft;
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
        /// Return a list of tracked aircraft that are candidates for API lookup
        /// </summary>
        /// <returns></returns>
        public async Task<List<TrackedAircraft>> ListLookupCandidatesAsync()
        {
            var eligibilityPredicate = EligibleForLookup(_maximumLookups);
            var aircraft = await ListAsync(eligibilityPredicate);
            return aircraft;
        }

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
                // The lookup properties may be set on the database but not in the incoming template
                // so make sure the value from the database is retained
                template.LookupTimestamp ??= aircraft.LookupTimestamp;
                template.LookupAttempts = aircraft.LookupAttempts;

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
        /// <param name="successful"></param>
        /// <returns></returns>
        public async Task<TrackedAircraft> UpdateLookupProperties(string address, bool successful)
        {
            // Get the list of eligible records and find the one for the specified aircraft
            var eligibilityPredicate = EligibleForLookup(_maximumLookups);
            var aircraft = await _context.TrackedAircraft
                                         .Where(eligibilityPredicate)
                                         .FirstOrDefaultAsync(x => x.Address == address);

            if (aircraft != null)
            {
                _logger.LogMessage(Severity.Debug,
                    $"Record found for lookup property update:" +
                    $"Address = {aircraft.Address}, " +
                    $"Callsign = {aircraft.Callsign}, " +
                    $"Lookup Attempts = {aircraft.LookupAttempts}, " +
                    $"Lookup Timestamp = {aircraft.LookupTimestamp}");

                // Increment the lookup attempt count
                aircraft.LookupAttempts += 1;

                // If the lookup was successful or the lookup attempt limit's been hit, set the lookup timestamp
                // to suppress further lookups
                if (successful || (aircraft.LookupAttempts >= _maximumLookups))
                {
                    aircraft.LookupTimestamp = DateTime.Now;
                }

                await _context.SaveChangesAsync();
            }
            else
            {
                _logger.LogMessage(Severity.Warning, $"Record for aircraft {address} not found for lookup property update");
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

        /// <summary>
        /// Define a predicate for filtering a collection of tracked aircraft records to identify those
        /// that are eligible for lookup:
        /// 
        /// 1. The address is populated
        /// 2. A lookup hasn't already been completed successfully
        /// 3. The record isn't locked
        /// 4. The maximum lookup attempts haven't been reached
        /// 
        /// The locking state criterion is necessary as it ensures a given aircraft address is unique in
        /// the returned results (an aircraft can only appear once in a non-locked state but may occur
        /// multiple times in a locked state, from multiple sessions).
        /// </summary>
        /// <param name="maximumLookups"></param>
        /// <returns></returns>
        private static Expression<Func<TrackedAircraft, bool>> EligibleForLookup(int maximumLookups)
        {
            return x =>
                !string.IsNullOrEmpty(x.Address) &&
                (x.LookupTimestamp == null) &&
                (x.Status != TrackingStatus.Locked) &&
                (maximumLookups == 0 || x.LookupAttempts < maximumLookups);
        }
    }
}

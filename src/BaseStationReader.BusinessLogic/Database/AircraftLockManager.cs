using BaseStationReader.Entities.Tracking;
using BaseStationReader.Interfaces.Database;
using BaseStationReader.Interfaces.Tracking;

namespace BaseStationReader.BusinessLogic.Database
{
    internal class AircraftLockManager : IAircraftLockManager
    {
        private readonly ITrackedAircraftWriter _writer;
        private readonly int _timeToLock;

        public AircraftLockManager(ITrackedAircraftWriter writer, int timeToLockMs)
        {
            _writer = writer;
            _timeToLock = timeToLockMs;
        }

        /// <summary>
        /// Get the active aircraft with the specified address
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public async Task<TrackedAircraft> GetActiveAircraftAsync(string address)
            => await GetActiveAircraftAsync(address, null, matchSession: false);

        /// <summary>
        /// Get the active aircraft with the specified address in the specified observation session.
        /// </summary>
        public async Task<TrackedAircraft> GetActiveAircraftAsync(string address, int sessionId, CancellationToken cancellationToken = default)
            => await GetActiveAircraftAsync(address, sessionId, matchSession: true, cancellationToken);

        private async Task<TrackedAircraft> GetActiveAircraftAsync(
            string address,
            int? sessionId,
            bool matchSession,
            CancellationToken cancellationToken = default)
        {
            // Get the aircraft. This method is guaranteed to return the most recent record for a given aircraft
            // address
            TrackedAircraft aircraft = matchSession
                ? await _writer.GetAsync(x => x.Address == address && x.SessionId == sessionId, cancellationToken)
                : await _writer.GetAsync(x => x.Address == address, cancellationToken);

            // If the last seen date has exceeded the time to lock timeout, this record should no longer be active
            if (aircraft != null && (DateTime.Now - aircraft.LastSeen).TotalMilliseconds >= _timeToLock)
            {
                // Timeout has been exceeded, so lock the record and return null
                aircraft.Status = TrackingStatus.Locked;
                await _writer.WriteAsync(aircraft, cancellationToken);
                aircraft = null;
            }

            return aircraft;
        }
    }
}

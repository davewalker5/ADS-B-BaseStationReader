using BaseStationReader.Entities.Interfaces;
using BaseStationReader.Entities.Tracking;

namespace BaseStationReader.BusinessLogic.Database
{
    public class AircraftLockManager : IAircraftLockManager
    {
        private readonly IAircraftWriter _writer;
        private readonly int _timeToLock;

        public AircraftLockManager(IAircraftWriter writer, int timeToLockMs)
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
        {
            // Get the aircraft. This method is guaranteed to return the most recent record for a given aircraft
            // address
            TrackedAircraft aircraft = await _writer.GetAsync(x => x.Address == address);

            // If the last seen date has exceeded the time to lock timeout, this record should no longer be active
            if (aircraft != null && (DateTime.Now - aircraft.LastSeen).TotalMilliseconds >= _timeToLock)
            {
                // Timeout has been exceeded, so lock the record and return null
                aircraft.Status = TrackingStatus.Locked;
                await _writer.WriteAsync(aircraft);
                aircraft = null;
            }

            return aircraft;
        }
    }
}

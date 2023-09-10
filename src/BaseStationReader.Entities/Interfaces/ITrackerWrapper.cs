using BaseStationReader.Entities.Events;
using BaseStationReader.Entities.Tracking;
using System.Collections.Concurrent;

namespace BaseStationReader.Entities.Interfaces
{
    public interface ITrackerWrapper
    {
        event EventHandler<AircraftNotificationEventArgs>? AircraftAdded;
        event EventHandler<AircraftNotificationEventArgs>? AircraftRemoved;
        event EventHandler<AircraftNotificationEventArgs>? AircraftUpdated;

        ConcurrentDictionary<string, Aircraft> TrackedAircraft { get; }
        void Initialise();
        void Start();
        void Stop();
    }
}
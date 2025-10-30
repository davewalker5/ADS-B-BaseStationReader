using BaseStationReader.Entities.Events;
using BaseStationReader.Entities.Tracking;
using System.Collections.Concurrent;

namespace BaseStationReader.Interfaces.Tracking
{
    public interface ITrackerController
    {
        event EventHandler<AircraftNotificationEventArgs> AircraftAdded;
        event EventHandler<AircraftNotificationEventArgs> AircraftRemoved;
        event EventHandler<AircraftNotificationEventArgs> AircraftUpdated;

        ConcurrentDictionary<string, TrackedAircraft> TrackedAircraft { get; }
        Task InitialiseAsync();
        void Start();
        void Stop();
        int QueueSize { get; }
        Task FlushQueueAsync();
        void ClearQueue();
    }
}
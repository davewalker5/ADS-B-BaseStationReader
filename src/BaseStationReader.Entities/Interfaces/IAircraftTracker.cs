using BaseStationReader.Entities.Events;

namespace BaseStationReader.Entities.Interfaces
{
    public interface IAircraftTracker
    {
        event EventHandler<AircraftNotificationEventArgs> AircraftAdded;
        event EventHandler<AircraftNotificationEventArgs> AircraftUpdated;
        event EventHandler<AircraftNotificationEventArgs> AircraftRemoved;

        bool IsTracking { get; }
        void Start();
        void Stop();

    }
}
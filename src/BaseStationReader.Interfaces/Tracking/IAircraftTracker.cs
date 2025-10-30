using BaseStationReader.Entities.Events;

namespace BaseStationReader.Interfaces.Tracking
{
    public interface IAircraftTracker
    {
        event EventHandler<AircraftNotificationEventArgs> AircraftAdded;
        event EventHandler<AircraftNotificationEventArgs> AircraftUpdated;
        event EventHandler<AircraftNotificationEventArgs> AircraftRemoved;

        Task StartAsync(CancellationToken token);
    }
}
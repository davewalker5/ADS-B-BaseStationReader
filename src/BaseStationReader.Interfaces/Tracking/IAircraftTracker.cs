using BaseStationReader.Entities.Events;

namespace BaseStationReader.Interfaces.Tracking
{
    public interface IAircraftTracker
    {
        event EventHandler<AircraftNotificationEventArgs> AircraftEvent;
        Task StartAsync(CancellationToken token);
    }
}
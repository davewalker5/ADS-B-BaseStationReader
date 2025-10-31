using BaseStationReader.Entities.Events;

namespace BaseStationReader.Interfaces.Tracking
{
    public interface ITrackerController
    {
        event EventHandler<AircraftNotificationEventArgs> AircraftAdded;
        event EventHandler<AircraftNotificationEventArgs> AircraftRemoved;
        event EventHandler<AircraftNotificationEventArgs> AircraftUpdated;

        Task StartAsync(CancellationToken token);
        int QueueSize { get; }
        Task FlushQueueAsync();
    }
}
using BaseStationReader.Entities.Events;
using BaseStationReader.Entities.Hub;

namespace BaseStationReader.Interfaces.Tracking
{
    public interface ITrackerController
    {
        event EventHandler<AircraftNotificationEventArgs> AircraftEvent;

        IEnumerable<TrackedAircraftDto> State { get; }

        Task StartAsync(CancellationToken token);
        int QueueSize { get; }
        Task FlushQueueAsync();
    }
}
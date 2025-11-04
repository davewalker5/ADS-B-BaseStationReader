using BaseStationReader.Entities.Events;

namespace BaseStationReader.TrackerHub.Interfaces
{
    public interface IEventBridge
    {
        ValueTask PublishAsync(AircraftNotificationEventArgs aircraftEvent, CancellationToken token = default);
    }
}
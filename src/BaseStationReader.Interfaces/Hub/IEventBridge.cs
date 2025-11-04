using BaseStationReader.Entities.Events;

namespace BaseStationReader.Interfaces.Hub
{
    public interface IEventBridge
    {
        ValueTask PublishAsync(AircraftNotificationEventArgs aircraftEvent, CancellationToken token = default);
    }
}
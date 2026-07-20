using BaseStationReader.Entities.Events;
using BaseStationReader.Entities.Hub;

namespace BaseStationReader.Interfaces.Hub
{
    public interface IEventBridge
    {
        ValueTask PublishAsync(AircraftNotificationEventArgs aircraftEvent, CancellationToken token = default);
        ValueTask PublishResetAsync(TrackingOptions options, CancellationToken token = default);
    }
}

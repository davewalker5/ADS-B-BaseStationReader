using BaseStationReader.Entities.Events;

namespace BaseStationReader.Interfaces.Tracking
{
    public interface IAircraftTracker
    {
        event EventHandler<AircraftNotificationEventArgs> AircraftEvent;

        /// <summary>
        /// Gets the number of accepted receiver messages processed during this tracker run.
        /// </summary>
        long MessagesProcessed => 0;

        /// <summary>
        /// Gets the number of aircraft addition events raised during this tracker run.
        /// </summary>
        long AircraftAdded => 0;

        /// <summary>
        /// Gets the number of aircraft removal events raised during this tracker run.
        /// </summary>
        long AircraftRemoved => 0;

        Task StartAsync(CancellationToken token);
    }
}

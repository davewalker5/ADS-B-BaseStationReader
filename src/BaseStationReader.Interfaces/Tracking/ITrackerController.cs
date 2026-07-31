using BaseStationReader.Entities.Events;
using BaseStationReader.Entities.Hub;

namespace BaseStationReader.Interfaces.Tracking
{
    public interface ITrackerController
    {
        event EventHandler<AircraftNotificationEventArgs> AircraftEvent;

        IEnumerable<TrackedAircraftDto> State { get; }
        TrackingOptions TrackingOptions {get; }

        /// <summary>
        /// Gets the number of accepted receiver messages processed during the current session.
        /// </summary>
        long MessagesProcessed => 0;

        /// <summary>
        /// Gets the number of aircraft position records successfully written during the current session.
        /// </summary>
        long PositionRecordsWritten => 0;

        /// <summary>
        /// Gets the number of aircraft added to tracking during the current session.
        /// </summary>
        long AircraftAdded => 0;

        /// <summary>
        /// Gets the number of aircraft removed from tracking during the current session.
        /// </summary>
        long AircraftRemoved => 0;

        /// <summary>Gets the number of distinct ICAO addresses observed during the current session.</summary>
        long DistinctAircraft => 0;

        /// <summary>Gets the number of distinct callsigns observed during the current session.</summary>
        long DistinctCallsigns => 0;

        /// <summary>Gets the number of distinct aircraft with successfully written position records.</summary>
        long AircraftWithPositionRecords => 0;

        Task StartAsync(CancellationToken token);
        int QueueSize { get; }
        Task FlushQueueAsync();
    }
}

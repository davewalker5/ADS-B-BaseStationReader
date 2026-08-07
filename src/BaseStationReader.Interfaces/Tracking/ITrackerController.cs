using BaseStationReader.Entities.Events;
using BaseStationReader.Entities.Hub;
using BaseStationReader.Entities.Spool;

namespace BaseStationReader.Interfaces.Tracking
{
    public interface ITrackerController
    {
        event EventHandler<AircraftNotificationEventArgs> AircraftEvent;

        /// <summary>Raised for every non-empty raw message received from the configured feed.</summary>
        event EventHandler<MessageReadEventArgs> MessageReceived { add { } remove { } }

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

        /// <summary>Gets the number of accepted position observations during the current session.</summary>
        long PositionRecordsObserved => PositionRecordsWritten;

        /// <summary>Gets the number of distinct ICAO addresses observed during the current session.</summary>
        long DistinctAircraft => 0;

        /// <summary>Gets the number of distinct callsigns observed during the current session.</summary>
        long DistinctCallsigns => 0;

        /// <summary>Gets the number of distinct aircraft with successfully written position records.</summary>
        long AircraftWithPositionRecords => 0;

        /// <summary>Gets the number of distinct aircraft with accepted position observations.</summary>
        long AircraftWithPositionObservations => AircraftWithPositionRecords;

        Task StartAsync(CancellationToken token);
        int QueueSize { get; }
        Task FlushQueueAsync(CancellationToken cancellationToken = default, IProgress<QueueFlushProgress> progress = null);

        /// <summary>Immediately asks session-owned components to reject and cancel outstanding work.</summary>
        void RequestStop() { }

        /// <summary>Overrides persistence behavior for the next controller stop.</summary>
        void ConfigureStopFlush(bool flushQueue, CancellationToken cancellationToken = default,
            IProgress<QueueFlushProgress> progress = null) { }
    }
}

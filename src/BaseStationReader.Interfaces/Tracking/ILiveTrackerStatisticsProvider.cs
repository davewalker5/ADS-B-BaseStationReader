namespace BaseStationReader.Interfaces.Tracking;

/// <summary>
/// Provides cumulative statistics for the current or most recently completed tracking session.
/// </summary>
public interface ILiveTrackerStatisticsProvider
{
    /// <summary>Gets the UTC timestamp of the latest accepted receiver message in this session.</summary>
    DateTime? LastActivityUtc => null;

    long PositionRecordsWritten { get; }
    long PositionRecordsObserved => PositionRecordsWritten;
    long MessagesProcessed { get; }
    long DistinctAircraft { get; }
    long DistinctCallsigns { get; }
    long AircraftWithPositionRecords { get; }
    long AircraftWithPositionObservations => AircraftWithPositionRecords;
}

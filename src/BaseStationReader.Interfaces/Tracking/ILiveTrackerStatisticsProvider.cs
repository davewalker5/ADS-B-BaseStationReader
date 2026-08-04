namespace BaseStationReader.Interfaces.Tracking;

/// <summary>
/// Provides cumulative statistics for the current or most recently completed tracking session.
/// </summary>
public interface ILiveTrackerStatisticsProvider
{
    long AircraftAdded { get; }
    long AircraftRemoved { get; }
    long PositionRecordsWritten { get; }
    long MessagesProcessed { get; }
    long DistinctAircraft { get; }
    long DistinctCallsigns { get; }
    long AircraftWithPositionRecords { get; }
}

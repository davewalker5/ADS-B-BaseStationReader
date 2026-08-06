namespace BaseStationReader.Entities.Spool;

/// <summary>Describes progress while a persistent writer queue is being flushed.</summary>
public sealed record QueueFlushProgress(int InitialCount, int RemainingCount)
{
    public int ProcessedCount => Math.Max(0, InitialCount - RemainingCount);
}

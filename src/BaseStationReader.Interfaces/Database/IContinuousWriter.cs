namespace BaseStationReader.Interfaces.Database
{
    public interface IContinuousWriter : IAsyncDisposable
    {
        int QueueSize { get; }

        /// <summary>
        /// Gets the number of aircraft position records successfully written during this run.
        /// </summary>
        long PositionRecordsWritten => 0;

        /// <summary>
        /// Gets the number of distinct aircraft for which a position record was successfully written.
        /// </summary>
        long AircraftWithPositionRecords => 0;

        void Push(object entity);
        Task StartAsync(CancellationToken token);

        /// <summary>
        /// Immediately rejects new records and cancels the active writer operation.
        /// </summary>
        void RequestStop();

        Task StopAsync(bool? flushOnStop = null, CancellationToken cancellationToken = default,
            IProgress<BaseStationReader.Entities.Spool.QueueFlushProgress> progress = null);
        Task FlushQueueAsync(CancellationToken cancellationToken = default,
            IProgress<BaseStationReader.Entities.Spool.QueueFlushProgress> progress = null);
    }
}

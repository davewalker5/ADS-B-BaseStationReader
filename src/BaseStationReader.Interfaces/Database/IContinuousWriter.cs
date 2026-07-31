namespace BaseStationReader.Interfaces.Database
{
    public interface IContinuousWriter : IAsyncDisposable
    {
        int QueueSize { get; }

        /// <summary>
        /// Gets the number of aircraft position records successfully written during this run.
        /// </summary>
        long PositionRecordsWritten => 0;

        void Push(object entity);
        Task StartAsync(CancellationToken token);
        Task StopAsync();
        Task FlushQueueAsync();
    }
}

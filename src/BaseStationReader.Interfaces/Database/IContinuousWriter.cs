namespace BaseStationReader.Interfaces.Database
{
    public interface IContinuousWriter : IAsyncDisposable
    {
        int QueueSize { get; }
        void Push(object entity);
        Task StartAsync(CancellationToken token);
        Task StopAsync();
        Task FlushQueueAsync();
    }
}
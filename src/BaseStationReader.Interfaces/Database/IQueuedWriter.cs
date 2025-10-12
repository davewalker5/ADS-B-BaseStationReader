using BaseStationReader.Entities.Events;

namespace BaseStationReader.Interfaces.Database
{
    public interface IQueuedWriter
    {
        event EventHandler<BatchWrittenEventArgs> BatchWritten;
        int QueueSize { get; }
        void Push(object entity);
        Task StartAsync();
        void Stop();
        void ClearQueue();
        Task FlushQueueAsync();
    }
}

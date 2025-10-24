using BaseStationReader.Entities.Events;

namespace BaseStationReader.Interfaces.Database
{
    public interface IQueuedWriter
    {
        event EventHandler<BatchStartedEventArgs> BatchStarted;
        event EventHandler<BatchCompletedEventArgs> BatchCompleted;
        int QueueSize { get; }
        void Push(object entity);
        Task StartAsync();
        void Stop();
        void ClearQueue();
        Task FlushQueueAsync();
    }
}

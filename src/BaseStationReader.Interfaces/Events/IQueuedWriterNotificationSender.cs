using BaseStationReader.Entities.Events;

namespace BaseStationReader.Interfaces.Events
{
    [Obsolete]
    public interface IQueuedWriterNotificationSender
    {
        void SendBatchStartedNotification(
            object sender,
            EventHandler<BatchStartedEventArgs> handlers,
            int queueSize);

        void SendBatchCompletedNotification(
            object sender,
            EventHandler<BatchCompletedEventArgs> handlers,
            int initialQueueSize,
            int finalQueueSize,
            int totalProcessed,
            long elapsedMillisconds);
    }
}
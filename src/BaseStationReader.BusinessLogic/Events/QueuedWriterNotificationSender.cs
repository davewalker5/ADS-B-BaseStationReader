
using BaseStationReader.Entities.Events;
using BaseStationReader.Interfaces.Events;
using BaseStationReader.Interfaces.Logging;

namespace BaseStationReader.BusinessLogic.Events
{
    public class QueuedWriterNotificationSender : SubscriberNotifier, IQueuedWriterNotificationSender
    {
        public QueuedWriterNotificationSender(ITrackerLogger logger) : base(logger)
        {
            
        }

        /// <summary>
        /// Send a batch started notification
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="handlers"></param>
        /// <param name="queueSize"></param>
        public void SendBatchStartedNotification(
            object sender,
            EventHandler<BatchStartedEventArgs> handlers,
            int queueSize)
        {
            var eventArgs = new BatchStartedEventArgs()
            {
                QueueSize = queueSize
            };

            NotifySubscribers(sender, handlers, eventArgs);
        }

        /// <summary>
        /// Send a batch completed notification
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="handlers"></param>
        /// <param name="initialQueueSize"></param>
        /// <param name="finalQueueSize"></param>
        /// <param name="totalProcessed"></param>
        /// <param name="elapsedMillisconds"></param>
        public void SendBatchCompletedNotification(
            object sender,
            EventHandler<BatchCompletedEventArgs> handlers,
            int initialQueueSize,
            int finalQueueSize,
            int totalProcessed,
            long elapsedMillisconds)
        {
            var eventArgs = new BatchCompletedEventArgs
            {
                InitialQueueSize = initialQueueSize,
                FinalQueueSize = finalQueueSize,
                EntriesProcessed = totalProcessed,
                Duration = elapsedMillisconds
            };

            NotifySubscribers(sender, handlers, eventArgs);
        }
    }
}
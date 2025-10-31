using BaseStationReader.Entities.Events;
using BaseStationReader.Interfaces.Events;
using BaseStationReader.Interfaces.Logging;

namespace BaseStationReader.BusinessLogic.Events
{
    public class MessageReaderNotificationSender : SubscriberNotifier, IMessageReaderNotificationSender
    {
        public MessageReaderNotificationSender(ITrackerLogger logger) : base(logger)
        {

        }

        /// <summary>
        /// Send a new message read notification
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="handlers"></param>
        /// <param name="message"></param>
        public void SendMessageReadNotification(object sender, EventHandler<MessageReadEventArgs> handlers, string message)
        {
            var eventArgs = new MessageReadEventArgs
            {
                Message = message
            };

            NotifySubscribers(sender, handlers, eventArgs);
        }
    }
}
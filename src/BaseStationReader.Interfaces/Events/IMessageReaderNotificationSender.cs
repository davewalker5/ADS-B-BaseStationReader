using BaseStationReader.Entities.Events;

namespace BaseStationReader.Interfaces.Events
{
    public interface IMessageReaderNotificationSender
    {
        void SendMessageReadNotification(object sender, EventHandler<MessageReadEventArgs> handlers, string message);
    }
}
using BaseStationReader.Entities.Events;

namespace BaseStationReader.Interfaces.Messages
{
    public interface IMessageReader
    {
        event EventHandler<MessageReadEventArgs> MessageRead;
        Task StartAsync(CancellationToken token);
    }
}
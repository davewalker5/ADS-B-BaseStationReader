using BaseStationReader.Entities.Events;

namespace BaseStationReader.Entities.Interfaces
{
    public interface IMessageReader
    {
        event EventHandler<MessageReadEventArgs> MessageRead;
        Task StartAsync(CancellationToken token);
    }
}
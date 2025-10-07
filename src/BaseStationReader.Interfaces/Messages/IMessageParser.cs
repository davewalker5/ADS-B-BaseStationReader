using BaseStationReader.Entities.Messages;

namespace BaseStationReader.Interfaces.Messages
{
    public interface IMessageParser
    {
        Message Parse(string[] fields);
    }
}
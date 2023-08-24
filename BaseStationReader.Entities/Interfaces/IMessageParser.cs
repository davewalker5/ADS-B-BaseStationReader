using BaseStationReader.Entities.Messages;

namespace BaseStationReader.Entities.Interfaces
{
    public interface IMessageParser
    {
        Message Parse(string[] fields);
    }
}
using BaseStationReader.Entities.Messages;

namespace BaseStationReader.Entities.Interfaces
{
    public interface IMessageGenerator
    {
        Message Generate(string address, string callsign, string squawk);
    }
}
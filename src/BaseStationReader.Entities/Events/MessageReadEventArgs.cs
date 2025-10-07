using System.Diagnostics.CodeAnalysis;

namespace BaseStationReader.Entities.Events
{
    [ExcludeFromCodeCoverage]
    public class MessageReadEventArgs : EventArgs
    {
        public string Message {get; set;} = "";
    }
}
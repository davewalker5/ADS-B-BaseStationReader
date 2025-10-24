using System.Diagnostics.CodeAnalysis;

namespace BaseStationReader.Entities.Events
{
    [ExcludeFromCodeCoverage]
    public class BatchStartedEventArgs : EventArgs
    {
        public int QueueSize { get; set; }
    }
}

using System.Diagnostics.CodeAnalysis;

namespace BaseStationReader.Entities.Events
{
    [ExcludeFromCodeCoverage]
    public class BatchCompletedEventArgs : EventArgs
    {
        public int InitialQueueSize { get; set; }
        public int FinalQueueSize { get; set; }
        public long Duration { get; set; }
    }
}

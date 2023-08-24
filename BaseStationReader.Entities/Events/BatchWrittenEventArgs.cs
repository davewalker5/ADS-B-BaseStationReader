using System.Diagnostics.CodeAnalysis;

namespace BaseStationReader.Entities.Events
{
    [ExcludeFromCodeCoverage]
    public class BatchWrittenEventArgs : EventArgs
    {
        public int InitialQueueSize { get; set; }
        public int FinalQueueSize { get; set; }
        public long Duration { get; set; }
    }
}

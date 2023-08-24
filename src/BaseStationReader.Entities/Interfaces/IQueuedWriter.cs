using BaseStationReader.Entities.Events;
using BaseStationReader.Entities.Tracking;

namespace BaseStationReader.Entities.Interfaces
{
    public interface IQueuedWriter
    {
        event EventHandler<BatchWrittenEventArgs>? BatchWritten;
        void Push(Aircraft aircraft);
        void Start();
        void Stop();
    }
}

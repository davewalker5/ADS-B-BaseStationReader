namespace BaseStationReader.Entities.Interfaces
{
    public interface ITrackerTimer
    {
        event EventHandler<EventArgs> Tick;
        void Start();
        void Stop();
    }
}

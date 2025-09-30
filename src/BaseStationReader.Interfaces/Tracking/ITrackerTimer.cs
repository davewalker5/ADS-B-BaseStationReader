namespace BaseStationReader.Interfaces.Tracking
{
    public interface ITrackerTimer
    {
        event EventHandler<EventArgs> Tick;
        void Start();
        void Stop();
    }
}

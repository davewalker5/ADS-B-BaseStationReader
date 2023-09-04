using BaseStationReader.Entities.Interfaces;
using System.Diagnostics.CodeAnalysis;
using System.Timers;

namespace BaseStationReader.Logic.Tracking
{
    [ExcludeFromCodeCoverage]
    public class TrackerTimer : ITrackerTimer
    {
        private System.Timers.Timer? _timer = null;
        private readonly double _interval;

        public event EventHandler<EventArgs>? Tick = null;

        public TrackerTimer(double interval)
        {
            _interval = interval;
        }

        public void Start()
        {
            _timer = new System.Timers.Timer(interval: _interval);
            _timer.Elapsed += OnElapsed;
            _timer.AutoReset = true;
            _timer.Enabled = true;
            _timer.Start();
        }

        public void Stop()
        {
            _timer?.Stop();
            _timer?.Dispose();
            _timer = null;
        }

        private void OnElapsed(object? sender, ElapsedEventArgs e)
        {
            Tick?.Invoke(this, e);
        }
    }
}

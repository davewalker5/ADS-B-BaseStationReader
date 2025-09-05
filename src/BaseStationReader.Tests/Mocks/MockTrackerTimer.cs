using BaseStationReader.Entities.Interfaces;
using System.Timers;

namespace BaseStationReader.Tests.Mocks
{
    internal class MockTrackerTimer : ITrackerTimer
    {
        private CancellationTokenSource _tokenSource = null;
        private readonly double _interval;

        public event EventHandler<EventArgs> Tick = null;

        public MockTrackerTimer(double interval)
        {
            _interval = interval;
        }

        public void Start()
        {
            _tokenSource = new CancellationTokenSource();
            Task.Run(() => EventLoop(_tokenSource.Token));
        }

        public void Stop()
        {
            _tokenSource?.Cancel();
        }

        private void EventLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                Thread.Sleep((int)_interval);
                Tick?.Invoke(this, new EventArgs());
            }
        }
    }
}

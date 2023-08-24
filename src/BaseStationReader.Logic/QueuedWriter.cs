using BaseStationReader.Entities.Events;
using BaseStationReader.Entities.Interfaces;
using BaseStationReader.Entities.Tracking;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace BaseStationReader.Logic
{
    public class QueuedWriter : IQueuedWriter
    {
        private readonly IAircraftManager _manager;
        private readonly ConcurrentQueue<Aircraft> _queue = new ConcurrentQueue<Aircraft>();
        private System.Timers.Timer? _timer;
        private readonly int _interval = 0;
        private readonly int _batchSize = 0;

        public event EventHandler<BatchWrittenEventArgs>? BatchWritten;

        public QueuedWriter(IAircraftManager manager, int interval, int batchSize)
        {
            _manager = manager;
            _interval = interval;
            _batchSize = batchSize;
        }

        /// <summary>
        /// Put aircraft details into the queue to be written
        /// </summary>
        /// <param name="aircraft"></param>
        public void Push(Aircraft aircraft)
        {
            // To stop the queue growing and consuming memory, entries are discarded if the timer
            // hasn't been started
            if (_timer != null)
            {
                _queue.Enqueue(aircraft);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public async void Start()
        {
            // Set the locked flag on all unlocked records. This prevents confusing tracking of the same aircraft
            // on different flights
            List<Aircraft> unlocked = await _manager.ListAsync(x => !x.Locked);
            foreach (var aircraft in unlocked)
            {
                aircraft.Locked = true;
                _queue.Enqueue(aircraft);
            }

            // Now start the timer
            _timer = new(interval: _interval);
            _timer.Elapsed += (sender, e) => OnTimer();
            _timer.AutoReset = true;
            _timer.Enabled = true;
            _timer?.Start();
        }

        /// <summary>
        /// Stop writing to the tracking database
        /// </summary>
        public void Stop()
        {
            _timer?.Stop();
            _timer?.Dispose();
            _queue.Clear();
        }

        /// <summary>
        /// When the timer fires, write the next batch of records to the database
        /// </summary>
        private async void OnTimer()
        {
#pragma warning disable CS8602 // Dereference of a possibly null reference.
            _timer.Stop();
#pragma warning restore CS8602 // Dereference of a possibly null reference.

            // Time how long the update takes
            Stopwatch stopwatch = Stopwatch.StartNew();

            // Iterate over at most the current batch size entries in the queue
            var initialQueueSize = _queue.Count;
            for (int i = 0; i < _batchSize; i++)
            {
                // Attempt to get the next item and if it's not there break out
                if (!_queue.TryDequeue(out Aircraft? queued))
                {
                    break;
                }

                // See if this is an existing aircraft for which the record hasn't been locked, to get the ID for update
                var existing = await _manager.GetAsync(x => (x.Address == queued.Address) && !x.Locked);
                if (existing != null)
                {
                    queued.Id = existing.Id;
                }

                // Write the data to the database
                await _manager.WriteAsync(queued);
            }
            stopwatch.Stop();
            var finalQueueSize = _queue.Count;
         
            BatchWritten?.Invoke(this, new BatchWrittenEventArgs{
                InitialQueueSize = initialQueueSize,
                FinalQueueSize = finalQueueSize,
                Duration = stopwatch.ElapsedMilliseconds
            });

            _timer.Start();
        }
    }
}

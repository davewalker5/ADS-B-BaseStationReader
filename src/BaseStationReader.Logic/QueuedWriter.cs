using BaseStationReader.Entities.Events;
using BaseStationReader.Entities.Interfaces;
using BaseStationReader.Entities.Logging;
using BaseStationReader.Entities.Tracking;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace BaseStationReader.Logic
{
    public class QueuedWriter : IQueuedWriter
    {
        private readonly IAircraftWriter _aircraftWriter;
        private readonly IPositionWriter _positionWriter;
        private readonly ConcurrentQueue<object> _queue = new ConcurrentQueue<object>();
        private readonly ITrackerLogger _logger;
        private readonly ITrackerTimer _timer;
        private readonly int _batchSize = 0;

        public event EventHandler<BatchWrittenEventArgs>? BatchWritten;

        public QueuedWriter(
            IAircraftWriter aircraftWriter,
            IPositionWriter positionWriter,
            ITrackerLogger logger,
            ITrackerTimer timer,
            int batchSize)
        {
            _aircraftWriter = aircraftWriter;
            _positionWriter = positionWriter;
            _logger = logger;
            _timer = timer;
            _timer.Tick += OnTimer;
            _batchSize = batchSize;
        }

        /// <summary>
        /// Put tracking details into the queue to be written
        /// </summary>
        /// <param name="aircraft"></param>
        public void Push(object entity)
        {
            // To stop the queue growing and consuming memory, entries are discarded if the timer
            // hasn't been started. Also, check the object being pushed is a valid tracking entity
            if ((_timer != null) && ((entity is Aircraft) || (entity is AircraftPosition)))
            {
                _queue.Enqueue(entity);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public async void Start()
        {
            // Set the locked flag on all unlocked records. This prevents confusing tracking of the same aircraft
            // on different flights
            List<Aircraft> unlocked = await _aircraftWriter.ListAsync(x => !x.Locked);
            foreach (var aircraft in unlocked)
            {
                aircraft.Locked = true;
                _queue.Enqueue(aircraft);
            }

            // Now start the timer
            _timer.Start();
        }

        /// <summary>
        /// Stop writing to the tracking database
        /// </summary>
        public void Stop()
        {
            _timer.Stop();
            _queue.Clear();
        }

        /// <summary>
        /// When the timer fires, write the next batch of records to the database
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnTimer(object? sender, EventArgs e)
        {
            _timer.Stop();

            // Time how long the update takes
            Stopwatch stopwatch = Stopwatch.StartNew();

            // Iterate over at most the current batch size entries in the queue
            var initialQueueSize = _queue.Count;
            for (int i = 0; i < _batchSize; i++)
            {
                // Attempt to get the next item and if it's not there break out
                if (!_queue.TryDequeue(out object? queued))
                {
                    break;
                }

                // Write the dequeued object to the database
                Task.Run(() => WriteDequeuedObject(queued)).Wait();
            }
            stopwatch.Stop();
            var finalQueueSize = _queue.Count;

            try
            {
                // Notify subscribers that a batch has been written
                BatchWritten?.Invoke(this, new BatchWrittenEventArgs
                {
                    InitialQueueSize = initialQueueSize,
                    FinalQueueSize = finalQueueSize,
                    Duration = stopwatch.ElapsedMilliseconds
                });
            }
            catch (Exception ex)
            {
                // Log and sink the exception. The writer has to be protected from errors in the
                // subscriber callbacks or the application will stop updating
                _logger.LogException(ex);
            }

            _timer.Start();
        }

        /// <summary>
        /// Receive a de-queued object, determine its type and use the appropriate writer to write it
        /// </summary>
        /// <param name="queued"></param>
        private async void WriteDequeuedObject(object queued)
        {
            // If it's an aircraft and it's an existing record that hasn't been locked, get the ID for update
            Aircraft? aircraft = queued as Aircraft;
            AircraftPosition? position = null;
            if (aircraft != null)
            {
                // See if this is an existing aircraft for which the record hasn't been locked, to get the ID for update
                aircraft.Id = await MatchAircraftAddress(aircraft.Address, false);
            }
            else
            {
                // Not an aircraft so it must be a position. If the aircraft Id isn't specified, try to find a match
                // for the address. Note that we allow matches to locked aircraft as the final position update may
                // come after the aircraft record has been locked
                position = queued as AircraftPosition;
                if (position != null)
                {
                    position.AircraftId = await MatchAircraftAddress(position.Address, true);
                }
            }

            // Write the data to the database
            try
            {
                if (aircraft != null)
                {
                    _logger.LogMessage(Severity.Debug, $"Writing aircraft {aircraft.Address} with Id {aircraft.Id}");
                    await _aircraftWriter.WriteAsync(aircraft);
                }
                else if ((position != null) && (position.AircraftId > 0))
                {
                    _logger.LogMessage(Severity.Debug, $"Writing position for aircraft with Id {position.AircraftId}");
                    await _positionWriter.WriteAsync(position);
                }
            }
            catch (Exception ex)
            {
                // Log and sink the exception. The writer needs to continue or the application will
                // stop writing to the database
                _logger.LogException(ex);
            }
        }

        /// <summary>
        /// Find an existing aircraft with the specified address and return it's Id, if found, or 0 if not found
        /// </summary>
        /// <param name="address"></param>
        /// <param name="matchLockedAircraft"></param>
        /// <returns></returns>
        private async Task<int> MatchAircraftAddress(string address, bool matchLockedAircraft)
        {
            int matchingId = 0;

            if (!string.IsNullOrEmpty(address))
            {
                var existing = await _aircraftWriter.GetAsync(x => (x.Address == address) && (matchLockedAircraft || !x.Locked));
                if (existing != null)
                {
                    matchingId = existing.Id;
                }
            }

            return matchingId;
        }
    }
}

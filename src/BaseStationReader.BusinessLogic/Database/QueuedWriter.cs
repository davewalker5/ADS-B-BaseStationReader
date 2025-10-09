using BaseStationReader.Entities.Events;
using BaseStationReader.Interfaces.Tracking;
using BaseStationReader.Entities.Logging;
using BaseStationReader.Entities.Tracking;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using BaseStationReader.Interfaces.Api;
using BaseStationReader.Interfaces.Database;
using BaseStationReader.Interfaces.Logging;
using BaseStationReader.Entities.Config;
using System.Diagnostics.CodeAnalysis;

namespace BaseStationReader.BusinessLogic.Database
{
    public class QueuedWriter : IQueuedWriter
    {
        private readonly ITrackedAircraftWriter _aircraftWriter;
        private readonly IPositionWriter _positionWriter;
        private readonly IAircraftLockManager _locker;
        private readonly IExternalApiWrapper _apiWrapper;
        private readonly ConcurrentQueue<object> _queue = new ConcurrentQueue<object>();
        private readonly ITrackerLogger _logger;
        private readonly ITrackerTimer _timer;
        private readonly int _batchSize = 0;
        private readonly bool _createSightings;
        private readonly IEnumerable<string> _departureAirportCodes;
        private readonly IEnumerable<string> _arrivalAirportCodes;

        public event EventHandler<BatchWrittenEventArgs> BatchWritten;
        
        public int QueueSize { get => _queue.Count; }

        public QueuedWriter(
            ITrackedAircraftWriter aircraftWriter,
            IPositionWriter positionWriter,
            IAircraftLockManager locker,
            IExternalApiWrapper apiWrapper,
            ITrackerLogger logger,
            ITrackerTimer timer,
            IEnumerable<string> departureAirportCodes,
            IEnumerable<string> arrivalArportCodes,
            int batchSize,
            bool createSightings)
        {
            _aircraftWriter = aircraftWriter;
            _positionWriter = positionWriter;
            _locker = locker;
            _apiWrapper = apiWrapper;
            _logger = logger;
            _timer = timer;
            _timer.Tick += OnTimer;
            _batchSize = batchSize;
            _createSightings = createSightings;
            _departureAirportCodes = departureAirportCodes;
            _arrivalAirportCodes = arrivalArportCodes;
        }

        /// <summary>
        /// Put tracking details into the queue to be written
        /// </summary>
        /// <param name="aircraft"></param>
        public void Push(object entity)
        {
            // To stop the queue growing and consuming memory, entries are discarded if the timer
            // hasn't been started. Also, check the object being pushed is a valid tracking entity
            if (_timer != null && (entity is TrackedAircraft || entity is AircraftPosition || entity is APILookupRequest))
            {
                _queue.Enqueue(entity);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public async Task StartAsync()
        {
            // Set the locked flag on all unlocked records. This prevents confusing tracking of the same aircraft
            // on different flights
            List<TrackedAircraft> unlocked = await _aircraftWriter.ListAsync(x => (x.Status != TrackingStatus.Locked));
            foreach (var aircraft in unlocked)
            {
                aircraft.Status = TrackingStatus.Locked;
                _queue.Enqueue(aircraft);
            }

            // Now start the timer
            _timer.Start();
        }

        /// <summary>
        /// Stop processing requests from the queue
        /// </summary>
        public void Stop()
        {
            _timer.Stop();
        }

        /// <summary>
        /// Flush all pending requests from the queue
        /// </summary>
        /// <returns></returns>
        public async Task FlushQueue()
        {
            do
            {
                await ProcessBatch(int.MaxValue);
            }
            while (!_queue.IsEmpty);
        }

        /// <summary>
        /// Clear all pending entries from the queue
        /// </summary>
        public void ClearQueue()
            => _queue.Clear();

        /// <summary>
        /// When the timer fires, process the next batch from the queue
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnTimer(object sender, EventArgs e)
        {
            _timer.Stop();

            // Process the next batch from the queue
            Task.Run(() => ProcessBatch(_batchSize)).Wait();

            _timer.Start();
        }

        /// <summary>
        /// Process a batch from the queue
        /// </summary>
        /// <param name="batchSize"></param>
        /// <returns></returns>
        private async Task ProcessBatch(int batchSize)
        {
            // Time how long the batch processing
            Stopwatch stopwatch = Stopwatch.StartNew();

            // Iterate over at most the current batch size entries in the queue
            var initialQueueSize = _queue.Count;
            for (int i = 0; i < batchSize; i++)
            {
                // Attempt to get the next item and if it's not there break out
                if (!_queue.TryDequeue(out object queued))
                {
                    break;
                }

                // Write the dequeued object to the database
                await HandleDequeuedObjectAsync(queued);
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
        }

        /// <summary>
        /// Receive a de-queued object, determine its type and use the handler to handle it
        /// </summary>
        /// <param name="queued"></param>
        private async Task HandleDequeuedObjectAsync(object queued)
        {
            try
            {
                var objectId = RuntimeHelpers.GetHashCode(queued);
                if (await WriteTrackedAircraft(queued, objectId)) return;
                if (await WriteAircraftPosition(queued, objectId)) return;
                await ProcessAPILookupRequest(queued, objectId);
            }
            catch (Exception ex)
            {
                // Log and sink the exception. The writer needs to continue or the application will
                // stop writing to the database
                _logger.LogException(ex);
            }
        }

        /// <summary>
        /// Attempt to handle a queued object as a tracked aircraft
        /// </summary>
        /// <param name="queued"></param>
        /// <param name="objectId"></param>
        /// <returns></returns>
        private async Task<bool> WriteTrackedAircraft(object queued, int objectId)
        {
            _logger.LogMessage(Severity.Verbose, $"Attempting to process queued object {objectId} as a tracked aircraft");

            // Attempt to cast the queued object as a tracked aircraft and identify if that's what it is
            if (queued is not TrackedAircraft aircraft)
            {
                _logger.LogMessage(Severity.Verbose, $"Queued object {objectId} is not a tracked aircraft");
                return false;
            }

                // See if it corresponds to an existing tracked aircraft record and, if so, set the aircraft
            // ID so that record will be updated rather than a new one created
            var activeAircraft = await _locker.GetActiveAircraftAsync(aircraft.Address);
            if (activeAircraft != null)
            {
                aircraft.Id = activeAircraft.Id;
            }

            // Write the tracked aircraft
            _logger.LogMessage(Severity.Verbose, $"Writing aircraft {aircraft.Address} with Id {aircraft.Id}");
            await _aircraftWriter.WriteAsync(aircraft);

            return true;
        }

        /// <summary>
        /// Attempt to handle a queued object as a tracked aircraft position
        /// </summary>
        /// <param name="queued"></param>
        /// <param name="objectId"></param>
        /// <returns></returns>
        private async Task<bool> WriteAircraftPosition(object queued, int objectId)
        {
            _logger.LogMessage(Severity.Verbose, $"Attempting to process queued object {objectId} as an aircraft position");

            // Attempt to cast the queued object as a position and identify if that's what it is
            if (queued is not AircraftPosition position)
            {
                _logger.LogMessage(Severity.Verbose, $"Queued object {objectId} is not an aircraft position");
                return false;
            }

            // Find the associated tracked aircraft
            var activeAircraft = await _locker.GetActiveAircraftAsync(position.Address);
            if (activeAircraft == null)
            {
                _logger.LogMessage(Severity.Debug, $"Aircraft with address {position.Address} is not active - API lookup will not be performed");
                _queue.Enqueue(position);
                return true;
            }

            // Assign the aircraft ID, for the foreign key relationship, and write the position
            position.AircraftId = activeAircraft.Id;
            _logger.LogMessage(Severity.Verbose, $"Writing position for aircraft {position.Address} with ID {position.AircraftId}");
            await _positionWriter.WriteAsync(position);

            return true;
        }

        /// <summary>
        /// Attempt to handle a queued object as a request to lookup a flight and aircraft via the
        /// external APIs
        /// </summary>
        /// <param name="queued"></param>
        /// <param name="objectId"></param>
        /// <returns></returns>
        [ExcludeFromCodeCoverage]
        private async Task<bool> ProcessAPILookupRequest(object queued, int objectId)
        {
            _logger.LogMessage(Severity.Verbose, $"Attempting to process queued object {objectId} as an API lookup request");

            // Attempt to cast the queued object as a lookup request and identify if that's what it is
            if (queued is not APILookupRequest request)
            {
                _logger.LogMessage(Severity.Verbose, $"Queued object {objectId} is not an API lookup request");
                return false;
            }

            // Find the associated tracked aircraft
            var activeAircraft = await _locker.GetActiveAircraftAsync(request.Address);
            if (activeAircraft == null)
            {
                _logger.LogMessage(Severity.Debug, $"Aircraft with address {request.Address} is not being tracked yet - API lookup will not be performed");
                _queue.Enqueue(request);
                return true;
            }

            // Check it's not already been looked up
            if (activeAircraft.LookupTimestamp != null)
            {
                _logger.LogMessage(Severity.Debug, $"Lookup for aircraft with address {request.Address} was completed at {activeAircraft.LookupTimestamp} - API lookup will not be performed");
                return true;
            }

            // Check the API wrapper has been initialised
            if (_apiWrapper == null)
            {
                _logger.LogMessage(Severity.Warning, $"Live API is not specified or is unsupported: Lookup for aircraft with address {request.Address} not done");
                return true;
            }

            // Perform the API lookup
            _logger.LogMessage(Severity.Debug, $"Performing API lookup for aircraft {request.Address}");
            var result = await _apiWrapper.LookupAsync(ApiEndpointType.ActiveFlights, request.Address, _departureAirportCodes, _arrivalAirportCodes, _createSightings);
            var outcome = result.Successful ? "was" : "was not";
            _logger.LogMessage(Severity.Info, $"Lookup for aircraft {request.Address} {outcome} successful");

            // Requeue the reques on unsuccessful lookups. The API wrapper will return false for the requeue indicator
            // if there's no point
            if (!result.Successful && result.Requeue)
            {
                _queue.Enqueue(request);
            }

            return true;
        }
    }
}

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
        private readonly IDatabaseManagementFactory _factory;
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
            IDatabaseManagementFactory factory,
            IExternalApiWrapper apiWrapper,
            ITrackerLogger logger,
            ITrackerTimer timer,
            IEnumerable<string> departureAirportCodes,
            IEnumerable<string> arrivalArportCodes,
            int batchSize,
            bool createSightings)
        {
            _factory = factory;
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
            if (_timer != null && (entity is TrackedAircraft || entity is AircraftPosition || entity is ApiLookupRequest))
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
            List<TrackedAircraft> unlocked = await _factory.TrackedAircraftWriter.ListAsync(x => (x.Status != TrackingStatus.Locked));
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
        public async Task FlushQueueAsync()
        {
            var initialQueueSize = _queue.Count;

            // Time how long the batch processing
            Stopwatch stopwatch = Stopwatch.StartNew();

            // Process pending tracked aircraft, position update and API lookup requests
            await ProcessPendingAsync<TrackedAircraft>();
            await ProcessPendingAsync<AircraftPosition>();
            await ProcessPendingAsync<ApiLookupRequest>();

            // Stop the timer
            stopwatch.Stop();

            // Notify subscribers that a batch has been processed
            NotifyBatchWrittenSubscribers(initialQueueSize, _queue.Count, stopwatch.ElapsedMilliseconds);
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
            Task.Run(() => ProcessBatchAsync(_batchSize)).Wait();

            _timer.Start();
        }

        /// <summary>
        /// Process a batch from the queue
        /// </summary>
        /// <param name="batchSize"></param>
        /// <returns></returns>
        private async Task ProcessBatchAsync(int batchSize)
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

                // Process the dequeued request
                await HandleDequeuedObjectAsync(queued, true);
            }
            stopwatch.Stop();

            // Notify subscribers that a batch has been processed
            NotifyBatchWrittenSubscribers(initialQueueSize, _queue.Count, stopwatch.ElapsedMilliseconds);
        }

        /// <summary>
        /// Process all pending requests of type T
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        private async Task ProcessPendingAsync<T>()
        {
            // Extract a list of requests from the queue
            var requests = _queue.OfType<T>();

            // Iterate over and process the requests
            foreach (var request in requests)
            {
                await HandleDequeuedObjectAsync(request, false);
            }
        }

        /// <summary>
        /// Notify subscribers that a batch has been processed from the queue
        /// </summary>
        /// <param name="initialQueueSize"></param>
        /// <param name="finalQueueSize"></param>
        /// <param name="elapsedMillisconds"></param>
        private void NotifyBatchWrittenSubscribers(int initialQueueSize, int finalQueueSize, long elapsedMillisconds)
        {
            try
            {
                // Notify subscribers that a batch has been written
                BatchWritten?.Invoke(this, new BatchWrittenEventArgs
                {
                    InitialQueueSize = initialQueueSize,
                    FinalQueueSize = finalQueueSize,
                    Duration = elapsedMillisconds
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
        /// <param name="allowRequeues"></param>
        private async Task HandleDequeuedObjectAsync(object queued, bool allowRequeues)
        {
            try
            {
                var objectId = RuntimeHelpers.GetHashCode(queued);
                if (await WriteTrackedAircraftAsync(queued, objectId)) return;
                if (await WriteAircraftPositionAsync(queued, objectId)) return;
                await ProcessAPILookupRequestAsync(queued, objectId, allowRequeues);
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
        private async Task<bool> WriteTrackedAircraftAsync(object queued, int objectId)
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
            var activeAircraft = await _factory.AircraftLockManager.GetActiveAircraftAsync(aircraft.Address);
            if (activeAircraft != null)
            {
                aircraft.Id = activeAircraft.Id;
            }

            // Write the tracked aircraft
            _logger.LogMessage(Severity.Verbose, $"Writing aircraft {aircraft.Address} with Id {aircraft.Id}");
            await _factory.TrackedAircraftWriter.WriteAsync(aircraft);

            return true;
        }

        /// <summary>
        /// Attempt to handle a queued object as a tracked aircraft position
        /// </summary>
        /// <param name="queued"></param>
        /// <param name="objectId"></param>
        /// <returns></returns>
        private async Task<bool> WriteAircraftPositionAsync(object queued, int objectId)
        {
            _logger.LogMessage(Severity.Verbose, $"Attempting to process queued object {objectId} as an aircraft position");

            // Attempt to cast the queued object as a position and identify if that's what it is
            if (queued is not AircraftPosition position)
            {
                _logger.LogMessage(Severity.Verbose, $"Queued object {objectId} is not an aircraft position");
                return false;
            }

            // Find the associated tracked aircraft
            var activeAircraft = await _factory.AircraftLockManager.GetActiveAircraftAsync(position.Address);
            if (activeAircraft == null)
            {
                _logger.LogMessage(Severity.Debug, $"Aircraft with address {position.Address} is not active - API lookup will not be performed");
                _queue.Enqueue(position);
                return true;
            }

            // Assign the aircraft ID, for the foreign key relationship, and write the position
            position.AircraftId = activeAircraft.Id;
            _logger.LogMessage(Severity.Verbose, $"Writing position for aircraft {position.Address} with ID {position.AircraftId}");
            await _factory.PositionWriter.WriteAsync(position);

            return true;
        }

        /// <summary>
        /// Attempt to handle a queued object as a request to lookup a flight and aircraft via the
        /// external APIs
        /// </summary>
        /// <param name="queued"></param>
        /// <param name="objectId"></param>
        /// <param name="allowRequeues"></param>
        /// <returns></returns>
        [ExcludeFromCodeCoverage]
        private async Task<bool> ProcessAPILookupRequestAsync(object queued, int objectId, bool allowRequeues)
        {
            _logger.LogMessage(Severity.Verbose, $"Attempting to process queued object {objectId} as an API lookup request");

            // Attempt to cast the queued object as a lookup request and identify if that's what it is
            if (queued is not ApiLookupRequest request)
            {
                _logger.LogMessage(Severity.Verbose, $"Queued object {objectId} is not an API lookup request");
                return false;
            }

            // Find the associated tracked aircraft
            var activeAircraft = await _factory.AircraftLockManager.GetActiveAircraftAsync(request.AircraftAddress);
            if (activeAircraft == null)
            {
                _logger.LogMessage(Severity.Debug, $"Aircraft with address {request.AircraftAddress} is not being tracked yet - API lookup will not be performed");
                _queue.Enqueue(request);
                return true;
            }

            // Check the API wrapper has been initialised
            if (_apiWrapper == null)
            {
                _logger.LogMessage(Severity.Warning, $"Live API is not specified or is unsupported: Lookup for aircraft with address {request.AircraftAddress} not done");
                return true;
            }

            // Populate additional lookup properties on the request
            request.FlightEndpointType = ApiEndpointType.ActiveFlights;
            request.DepartureAirportCodes = _departureAirportCodes;
            request.ArrivalAirportCodes = _arrivalAirportCodes;
            request.CreateSighting = _createSightings;

            // Perform the API lookup
            _logger.LogMessage(Severity.Debug, $"Performing API lookup for aircraft {request.AircraftAddress}");
            var result = await _apiWrapper.LookupAsync(request);
            var outcome = result.Successful ? "was" : "was not";
            _logger.LogMessage(Severity.Info, $"Lookup for aircraft {request.AircraftAddress} {outcome} successful");

            // Requeue the reques on unsuccessful lookups. The API wrapper will return false for the requeue indicator
            // if there's no point
            if (allowRequeues && !result.Successful && result.Requeue)
            {
                _queue.Enqueue(request);
            }

            return true;
        }
    }
}

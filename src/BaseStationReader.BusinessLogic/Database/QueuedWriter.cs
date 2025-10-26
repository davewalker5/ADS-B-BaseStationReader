using BaseStationReader.Entities.Events;
using BaseStationReader.Interfaces.Tracking;
using BaseStationReader.Entities.Logging;
using BaseStationReader.Entities.Tracking;
using System.Collections.Concurrent;
using System.Diagnostics;
using BaseStationReader.Interfaces.Api;
using BaseStationReader.Interfaces.Database;
using BaseStationReader.Entities.Config;
using System.Diagnostics.CodeAnalysis;

namespace BaseStationReader.BusinessLogic.Database
{
    public class QueuedWriter : IQueuedWriter
    {
        private readonly IDatabaseManagementFactory _factory;
        private readonly IExternalApiWrapper _apiWrapper;
        private readonly ConcurrentQueue<object> _queue = new ConcurrentQueue<object>();
        private readonly ITrackerTimer _timer;
        private readonly int _batchSize = 0;
        private readonly bool _createSightings;
        private bool _isProcessingBatch = false;
        private readonly IEnumerable<string> _departureAirportCodes;
        private readonly IEnumerable<string> _arrivalAirportCodes;

        public event EventHandler<BatchStartedEventArgs> BatchStarted;
        public event EventHandler<BatchCompletedEventArgs> BatchCompleted;
        
        public int QueueSize { get => _queue.Count; }

        public QueuedWriter(
            IDatabaseManagementFactory factory,
            IExternalApiWrapper apiWrapper,
            ITrackerTimer timer,
            IEnumerable<string> departureAirportCodes,
            IEnumerable<string> arrivalArportCodes,
            int batchSize,
            bool createSightings)
        {
            _factory = factory;
            _apiWrapper = apiWrapper;
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
        /// Start the queued writer
        /// </summary>
        public async Task StartAsync()
        {
            // Set the locked flag on all unlocked records. This prevents confusing tracking of the same aircraft
            // on different flights
            List<TrackedAircraft> unlocked = await _factory.TrackedAircraftWriter.ListAsync(x => x.Status != TrackingStatus.Locked);
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

            // It's conceivable a batch might be being processed - don't allow the flush to happen while
            // that's in progress as it'll cause conflicts writing to the database
            while (_isProcessingBatch)
            {
                Thread.Sleep(100);
            }

            // Set the "processing batch" flag
            _isProcessingBatch = true;

            // Notify subscribers that a batch is about to be processed
            NotifyBatchStartedSubscribers(initialQueueSize);

            // Time how long the batch processing
            Stopwatch stopwatch = Stopwatch.StartNew();

            // Process pending tracked aircraft, position update and API lookup requests
            var numberOfAircraft = await ProcessPendingAsync<TrackedAircraft>();
            var numberOfPositions = await ProcessPendingAsync<AircraftPosition>();
            var numberOfApiLookups = await ProcessPendingAsync<ApiLookupRequest>();

            // Clear the queue
            _queue.Clear();

            // Stop the timer
            stopwatch.Stop();

            // Clear the "processing batch" flag
            _isProcessingBatch = true;

            // Notify subscribers that a batch has been processed
            var totalProcessed = numberOfAircraft + numberOfPositions + numberOfApiLookups;
            NotifyBatchCompletedSubscribers(initialQueueSize, _queue.Count, totalProcessed, stopwatch.ElapsedMilliseconds);
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
            => Task.Run(() => ProcessBatchAsync(_batchSize)).Wait();

        /// <summary>
        /// Process a batch from the queue
        /// </summary>
        /// <param name="batchSize"></param>
        /// <returns></returns>
        private async Task ProcessBatchAsync(int batchSize)
        {
            // If we're already processing a batch, do nothing
            if (_isProcessingBatch)
            {
                return;
            }

            // Set the flag indicating a batch is being processed
            _isProcessingBatch = true;

            // Notify subscribers that a batch is about to be processed
            var initialQueueSize = _queue.Count;
            NotifyBatchStartedSubscribers(initialQueueSize);

            // Time how long the batch processing
            Stopwatch stopwatch = Stopwatch.StartNew();

            // Iterate over at most the current batch size entries in the queue
            int entriesProcessed = 0;
            for (int i = 0; i < batchSize; i++)
            {
                // Attempt to get the next item and if it's not there break out
                if (!_queue.TryDequeue(out object queued))
                {
                    break;
                }

                // Process the dequeued request
                entriesProcessed++;
                await HandleDequeuedObjectAsync(queued, true);
            }
            stopwatch.Stop();

            // Clear the flag indicating a batch is being processed
            _isProcessingBatch = false;

            // Notify subscribers that a batch has been processed
            NotifyBatchCompletedSubscribers(initialQueueSize, _queue.Count, entriesProcessed, stopwatch.ElapsedMilliseconds);
        }

        /// <summary>
        /// Process all pending requests of type T
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        private async Task<int> ProcessPendingAsync<T>()
        {
            // Extract a list of requests from the queue
            var requests = _queue.OfType<T>();

            // Iterate over and process the requests
            foreach (var request in requests)
            {
                await HandleDequeuedObjectAsync(request, false);
            }

            return requests.Count();
        }

        /// <summary>
        /// Notify subscribers that a batch is about to be processed from the queue
        /// </summary>
        /// <param name="queueSize"></param>
        private void NotifyBatchStartedSubscribers(int queueSize)
        {
            try
            {
                // Notify subscribers that batch processing is starting
                BatchStarted?.Invoke(this, new BatchStartedEventArgs
                {
                    QueueSize = queueSize
                });
            }
            catch (Exception ex)
            {
                // Log and sink the exception. The writer has to be protected from errors in the
                // subscriber callbacks or the application will stop updating
                _factory.Logger.LogException(ex);
            }
        }

        /// <summary>
        /// Notify subscribers that a batch has been processed from the queue
        /// </summary>
        /// <param name="initialQueueSize"></param>
        /// <param name="finalQueueSize"></param>
        /// <param name="totalProcessed"></param>
        /// <param name="elapsedMillisconds"></param>
        private void NotifyBatchCompletedSubscribers(int initialQueueSize, int finalQueueSize, int totalProcessed, long elapsedMillisconds)
        {
            try
            {
                // Notify subscribers that a batch has been written
                BatchCompleted?.Invoke(this, new BatchCompletedEventArgs
                {
                    InitialQueueSize = initialQueueSize,
                    FinalQueueSize = finalQueueSize,
                    EntriesProcessed = totalProcessed,
                    Duration = elapsedMillisconds
                });
            }
            catch (Exception ex)
            {
                // Log and sink the exception. The writer has to be protected from errors in the
                // subscriber callbacks or the application will stop updating
                _factory.Logger.LogException(ex);
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
                if (await WriteTrackedAircraftAsync(queued)) return;
                if (await WriteAircraftPositionAsync(queued)) return;
                await ProcessAPILookupRequestAsync(queued, allowRequeues);
            }
            catch (Exception ex)
            {
                // Log and sink the exception. The writer needs to continue or the application will
                // stop writing to the database
                _factory.Logger.LogException(ex);
            }
        }

        /// <summary>
        /// Attempt to handle a queued object as a tracked aircraft
        /// </summary>
        /// <param name="queued"></param>
        /// <param name="objectId"></param>
        /// <returns></returns>
        private async Task<bool> WriteTrackedAircraftAsync(object queued)
        {
            // Attempt to cast the queued object as a tracked aircraft and identify if that's what it is
            if (queued is not TrackedAircraft aircraft)
            {
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
            _factory.Logger.LogMessage(Severity.Verbose, $"Writing aircraft {aircraft.Address} with Id {aircraft.Id}");
            await _factory.TrackedAircraftWriter.WriteAsync(aircraft);

            return true;
        }

        /// <summary>
        /// Attempt to handle a queued object as a tracked aircraft position
        /// </summary>
        /// <param name="queued"></param>
        /// <returns></returns>
        private async Task<bool> WriteAircraftPositionAsync(object queued)
        {
            // Attempt to cast the queued object as a position and identify if that's what it is
            if (queued is not AircraftPosition position)
            {
                return false;
            }

            // Find the associated tracked aircraft. Aircraft are queued before their associated positions
            // and as it's a FIFO queue this should always return a valid aircraft. If the aircraft isn't
            // found, ignore the position record
            var activeAircraft = await _factory.AircraftLockManager.GetActiveAircraftAsync(position.Address);
            if (activeAircraft == null)
            {
                return true;
            }

            // Assign the aircraft ID, for the foreign key relationship, and write the position
            position.AircraftId = activeAircraft.Id;
            await _factory.PositionWriter.WriteAsync(position);

            return true;
        }

        /// <summary>
        /// Attempt to handle a queued object as a request to lookup a flight and aircraft via the
        /// external APIs
        /// </summary>
        /// <param name="queued"></param>
        /// <param name="allowRequeues"></param>
        /// <returns></returns>
        [ExcludeFromCodeCoverage]
        private async Task<bool> ProcessAPILookupRequestAsync(object queued, bool allowRequeues)
        {
            // Attempt to cast the queued object as a lookup request and identify if that's what it is
            if (queued is not ApiLookupRequest request)
            {
                return false;
            }

            // Find the associated tracked aircraft. Aircraft are queued before their associated positions
            // and as it's a FIFO queue this should always return a valid aircraft. If the aircraft isn't
            // found, ignore the API request
            var activeAircraft = await _factory.AircraftLockManager.GetActiveAircraftAsync(request.AircraftAddress);
            if (activeAircraft == null)
            {
                return true;
            }

            // Check the API wrapper has been initialised
            if (_apiWrapper == null)
            {
                _factory.Logger.LogMessage(Severity.Warning, $"Live API is not specified or is unsupported: Lookup for aircraft with address {request.AircraftAddress} not done");
                return true;
            }

            // Populate additional lookup properties on the request
            request.FlightEndpointType = ApiEndpointType.ActiveFlights;
            request.DepartureAirportCodes = _departureAirportCodes;
            request.ArrivalAirportCodes = _arrivalAirportCodes;
            request.CreateSighting = _createSightings;

            // Perform the API lookup
            _factory.Logger.LogMessage(Severity.Info, $"Performing API lookup for aircraft {request.AircraftAddress}");
            var result = await _apiWrapper.LookupAsync(request);
            var outcome = result.Successful ? "was" : "was not";
            _factory.Logger.LogMessage(Severity.Info, $"Lookup for aircraft {request.AircraftAddress} {outcome} successful");

            // If the lookup isn't successful, the API wrapper will return an indication of whether it's worth trying
            // again. For example, if the callsign, used in flight mapping, isn't currently available it might be set
            // by a subsequent ADS-B message at which point the lookup may succeed. If the result indicates a requeue
            // is worthwhile, requeue the request
            if (allowRequeues && !result.Successful && result.Requeue)
            {
                _queue.Enqueue(request);
            }

            return true;
        }
    }
}

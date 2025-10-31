using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using BaseStationReader.Entities.Logging;
using BaseStationReader.Entities.Tracking;
using BaseStationReader.Interfaces.Api;
using BaseStationReader.Interfaces.Database;

namespace BaseStationReader.BusinessLogic.Database
{
    public class ContinuousWriter : IContinuousWriter
    {
        private readonly ConcurrentQueue<object> _queue = new();
        private readonly SemaphoreSlim _signal = new(0, 1);
        private readonly IDatabaseManagementFactory _factory;
        private readonly IExternalApiWrapper _apiWrapper;

        private readonly IEnumerable<string> _departureAirportCodes;
        private readonly IEnumerable<string> _arrivalAirportCodes;
        private readonly bool _createSightings;
        private readonly object _gate = new();
        private CancellationTokenSource _source;
        private Task _runTask = null;
        private int _pending = 0;

        public int QueueSize { get => _queue.Count; }

        public ContinuousWriter(
            IDatabaseManagementFactory factory,
            IExternalApiWrapper apiWrapper,
            IEnumerable<string> departureAirportCodes,
            IEnumerable<string> arrivalArportCodes,
            bool createSightings)
        {
            _factory = factory;
            _apiWrapper = apiWrapper;
            _departureAirportCodes = departureAirportCodes;
            _arrivalAirportCodes = arrivalArportCodes;
            _createSightings = createSightings;
        }

        /// <summary>
        /// Push an object into the queue to be processed
        /// </summary>
        /// <param name="aircraft"></param>
        public void Push(object entity)
        {
            // To stop the queue growing and consuming memory, entries are discarded if the timer
            // hasn't been started. Also, check the object being pushed is a valid tracking entity
            if ((entity is TrackedAircraft) || (entity is AircraftPosition) || (entity is ApiLookupRequest))
            {
                _queue.Enqueue(entity);
                if (Interlocked.Increment(ref _pending) == 1)
                {
                    TryRelease();
                }
            }
        }

        /// <summary>
        /// Start the continuous writer
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public Task StartAsync(CancellationToken token)
        {
            lock (_gate)
            {
                // Check it's not already running
                if (_runTask is { IsCompleted: false })
                {
                    return Task.CompletedTask;
                }

                // Clear the queue
                _queue.Clear();

                // Create a cancellation token source linked to the token passed in. This ensures that
                // cancelling the token that's passed in will cancel this one, too
                _source = CancellationTokenSource.CreateLinkedTokenSource(token);

                // Keep a reference to the task that runs the continuous writer, so we can observe any faults
                _runTask = RunAsync(_source.Token);
                return Task.CompletedTask;
            }
        }

        /// <summary>
        /// Stop the continuous writer
        /// </summary>
        /// <returns></returns>
        public async Task StopAsync()
        {
            Task toAwait;

            // If there's no task running the async loop, there's nothing to do
            if (_runTask == null)
            {
                return;
            }

            lock (_gate)
            {
                // Cancel the internal, linked token, release the semaphore and make a copy of the run task
                // that can safely be awaited (otherwise, it's mutable and could be nulled mid-await)
                _source.Cancel();
                TryRelease();
                toAwait = _runTask;
            }

            try
            {
                // Wait for the runner to wind down
                await toAwait.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Expected when the token is cancelled
            }
            finally
            {
                lock (_gate)
                {
                    // Dispose the token source and tidy up
                    _runTask = null;
                    _source.Dispose();
                    _source = null;
                }
            }
        }

        /// <summary>
        /// Flush all pending requests from the queue
        /// </summary>
        /// <returns></returns>
        public async Task FlushQueueAsync()
        {
            _factory.Logger.LogMessage(Severity.Info, $"Flushing {_queue.Count} queued entries");

            // Process pending tracked aircraft, position update and API lookup requests
            await ProcessPendingAsync<TrackedAircraft>();
            await ProcessPendingAsync<AircraftPosition>();
            await ProcessPendingAsync<ApiLookupRequest>();

            // Clear the queue
            _queue.Clear();
        }

        /// <summary>
        /// IAsyncDisposable implementation
        /// </summary>
        /// <returns></returns>
        public async ValueTask DisposeAsync()
        {
            TryRelease();
            _signal.Dispose();
            await Task.CompletedTask;
        }

        /// <summary>
        /// Safe release of the semaphore
        /// </summary>
        private void TryRelease()
        {
            try
            {
                _signal.Release();
            }
            catch (SemaphoreFullException)
            {
                // Already signalled, so sink the exception
            }
        }

        /// <summary>
        /// Start processing
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        private async Task RunAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    // Sleep until at least one item is added to the queue
                    await _signal.WaitAsync(token);

                    // Drain everything that’s currently queued in strictly serial order, waiting until there's
                    // nothing remaining before breaking out of the loop
                    do
                    {
                        // Dequeue the next item
                        while (_queue.TryDequeue(out var item))
                        {
                            // Process it
                            await ProcessAsync(item).ConfigureAwait(false);
                            Interlocked.Decrement(ref _pending);
                        }
                    }
                    while (Volatile.Read(ref _pending) > 0);
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    // Log and sink the error
                    _factory.Logger.LogMessage(Severity.Error, ex.Message);
                    _factory.Logger.LogException(ex);
                }
            }
        }

        /// <summary>
        /// Process an iterm from the queue
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        private async Task ProcessAsync(object item)
        {
            try
            {
                if (item is TrackedAircraft aircraft)
                {
                    await WriteTrackedAircraftAsync(aircraft);
                }

                if (item is AircraftPosition position)
                {
                    await WriteAircraftPositionAsync(position);
                }

                if (item is ApiLookupRequest request)
                {
                    await ProcessAPILookupRequestAsync(item, false);
                }
            }
            catch (Exception ex)
            {
                // Log and sink the exception. The writer needs to continue or the application will
                // stop writing to the database
                _factory.Logger.LogException(ex);
            }
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
            _factory.Logger.LogMessage(Severity.Info, $"Processing {requests.Count()} queued entries of type {typeof(T).Name}");

            // Iterate over and process the requests
            foreach (var request in requests)
            {
                await ProcessAsync(request);
            }
        }

        /// <summary>
        /// Write a queued aircraft to the database
        /// </summary>
        /// <param name="aircraft"></param>
        /// <returns></returns>
        private async Task<bool> WriteTrackedAircraftAsync(TrackedAircraft aircraft)
        {
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
        /// Write a queued aircraft position to the database
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        private async Task<bool> WriteAircraftPositionAsync(AircraftPosition position)
        {
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
        /// Handle a request to process an API lookup for an aircraft and flight
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
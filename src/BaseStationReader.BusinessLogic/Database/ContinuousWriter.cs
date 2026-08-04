using System.Collections.Concurrent;
using BaseStationReader.Entities.Logging;
using BaseStationReader.Entities.History;
using BaseStationReader.Entities.Tracking;
using BaseStationReader.Interfaces.Database;

namespace BaseStationReader.BusinessLogic.Database
{
    public class ContinuousWriter : IContinuousWriter
    {
        private readonly ConcurrentQueue<object> _queue = new();
        private readonly SemaphoreSlim _signal = new(0, 1);
        private readonly IDatabaseManagementFactory _factory;
        private readonly object _gate = new();
        private CancellationTokenSource _source;
        private Task _runTask = null;
        private int _pending = 0;
        private long _positionRecordsWritten;
        private readonly ConcurrentDictionary<string, byte> _positionAircraft = new(StringComparer.OrdinalIgnoreCase);

        public int QueueSize { get => _queue.Count; }

        /// <inheritdoc />
        public long PositionRecordsWritten => Interlocked.Read(ref _positionRecordsWritten);

        /// <inheritdoc />
        public long AircraftWithPositionRecords => _positionAircraft.Count;

        public ContinuousWriter(IDatabaseManagementFactory factory)
        {
            _factory = factory;
        }

        /// <summary>
        /// Push an object into the queue to be processed
        /// </summary>
        /// <param name="aircraft"></param>
        public void Push(object entity)
        {
            // To stop the queue growing and consuming memory, entries are discarded if the timer
            // hasn't been started. Also, check the object being pushed is a valid tracking entity
            if ((entity is TrackedAircraft) || (entity is AircraftPosition) ||
                (entity is PositionDensitySnapshotEntity))
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
                Interlocked.Exchange(ref _pending, 0);

                // Each writer run belongs to one observation session, so begin its successful-write count at zero.
                Interlocked.Exchange(ref _positionRecordsWritten, 0);
                _positionAircraft.Clear();

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

                // Cancellation can stop the loop with queued work remaining, so finish it serially before disposal.
                await FlushQueueAsync().ConfigureAwait(false);
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

            // Drain in original FIFO order so snapshots remain behind the positions they represent.
            while (_queue.TryDequeue(out var item))
            {
                await ProcessAsync(item).ConfigureAwait(false);
                Interlocked.Decrement(ref _pending);
            }
        }

        /// <summary>
        /// IAsyncDisposable implementation
        /// </summary>
        /// <returns></returns>
        public async ValueTask DisposeAsync()
        {
            // Wait for the loop to stop then dispose the Semaphore
            await StopAsync().ConfigureAwait(false);
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
            catch (ObjectDisposedException)
            {
                // Shutting down, so sink the exception
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
                    // Shutting down, so sink the exception
                    break;
                }
                catch (ObjectDisposedException)
                {
                    // Semaphore disposed, shutting down, so sink the exception
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

                if (item is PositionDensitySnapshotEntity snapshot)
                {
                    await WritePositionDensitySnapshotAsync(snapshot);
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

            // Increment only after persistence succeeds so the status value describes records actually written.
            Interlocked.Increment(ref _positionRecordsWritten);
            _positionAircraft.TryAdd(position.Address, 0);

            return true;
        }

        /// <summary>
        /// Writes one complete queued position-density snapshot atomically.
        /// </summary>
        /// <param name="snapshot"></param>
        /// <returns></returns>
        private async Task<bool> WritePositionDensitySnapshotAsync(PositionDensitySnapshotEntity snapshot)
        {
            // The snapshot manager owns the transaction spanning the header and every populated cell.
            await _factory.PositionDensitySnapshotManager.AddAsync(snapshot);
            return true;
        }

    }
}

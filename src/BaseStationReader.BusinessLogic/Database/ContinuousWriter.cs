using System.Collections.Concurrent;
using BaseStationReader.Entities.History;
using BaseStationReader.Entities.Logging;
using BaseStationReader.Entities.Spool;
using BaseStationReader.Entities.Tracking;
using BaseStationReader.Interfaces.Database;
using BaseStationReader.Interfaces.Spool;

namespace BaseStationReader.BusinessLogic.Database
{
    public class ContinuousWriter : IContinuousWriter
    {
        private readonly ISpoolQueue _queue;
        private readonly SemaphoreSlim _signal = new(0, 1);
        private readonly IDatabaseManagementFactory _factory;
        private readonly bool _flushOnStop;
        private readonly bool _flushWhileActive;
        private readonly object _gate = new();
        private CancellationTokenSource _source;
        private Task _runTask = null;
        private volatile bool _accepting;
        private long _positionRecordsWritten;
        private readonly ConcurrentDictionary<string, byte> _positionAircraft = new(StringComparer.OrdinalIgnoreCase);

        public int QueueSize => _queue.Count;

        /// <inheritdoc />
        public long PositionRecordsWritten => Interlocked.Read(ref _positionRecordsWritten);

        /// <inheritdoc />
        public long AircraftWithPositionRecords => _positionAircraft.Count;

        /// <summary>
        /// Initialises a continuous writer over a persistent spool.
        /// </summary>
        /// <param name="factory">Database management factory.</param>
        /// <param name="queue">Persistent writer queue.</param>
        /// <param name="flushOnStop">Whether stopping should attempt every pending write.</param>
        /// <param name="flushWhileActive">Whether queued writes should be processed while tracking is active.</param>
        public ContinuousWriter(
            IDatabaseManagementFactory factory,
            ISpoolQueue queue,
            bool flushOnStop = true,
            bool flushWhileActive = true)
        {
            _factory = factory;
            _queue = queue;
            _flushOnStop = flushOnStop;
            _flushWhileActive = flushWhileActive;
        }

        /// <summary>
        /// Push an object into the queue to be processed
        /// </summary>
        /// <param name="entity"></param>
        public void Push(object entity)
        {
            if (_accepting && entity is TrackedAircraft or AircraftPosition or PositionDensitySnapshotEntity)
            {
                _queue.Enqueue(entity);
                TryRelease();
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

                // Each writer run belongs to one observation session, so begin its successful-write count at zero.
                Interlocked.Exchange(ref _positionRecordsWritten, 0);
                _positionAircraft.Clear();

                // Create a cancellation token source linked to the token passed in. This ensures that
                // cancelling the token that's passed in will cancel this one, too
                _source = CancellationTokenSource.CreateLinkedTokenSource(token);
                _accepting = true;

                // Keep a reference to the task that runs the continuous writer, so we can observe any faults
                _runTask = _flushWhileActive
                    ? RunAsync(_source.Token)
                    : Task.Delay(Timeout.InfiniteTimeSpan, _source.Token);
                if (_flushWhileActive && _queue.Count > 0)
                {
                    TryRelease();
                }

                return Task.CompletedTask;
            }
        }

        /// <summary>
        /// Stop the continuous writer
        /// </summary>
        /// <returns></returns>
        public async Task StopAsync(bool? flushOnStop = null, CancellationToken cancellationToken = default,
            IProgress<QueueFlushProgress> progress = null)
        {
            Task toAwait;

            // If there's no task running the async loop, there's nothing to do
            if (_runTask == null)
            {
                return;
            }

            RequestStop();

            lock (_gate)
            {
                // Keep a stable reference while the cancelled runner winds down.
                toAwait = _runTask;
            }

            try
            {
                // Wait for the runner to wind down.
                try
                {
                    await toAwait.ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    // Expected when the writer's run token is cancelled.
                }

                if (flushOnStop ?? _flushOnStop)
                {
                    await FlushQueueAsync(cancellationToken, progress).ConfigureAwait(false);
                }
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

        /// <inheritdoc />
        public void RequestStop()
        {
            lock (_gate)
            {
                // This method is intentionally synchronous so the stop initiator can close the
                // producer boundary before waiting for the receiver and other session components.
                _accepting = false;
                _source?.Cancel();
                TryRelease();
            }
        }

        /// <summary>
        /// Flush all pending requests from the queue
        /// </summary>
        /// <returns></returns>
        public async Task FlushQueueAsync(CancellationToken cancellationToken = default,
            IProgress<QueueFlushProgress> progress = null)
        {
            var initialCount = _queue.Count;
            _factory.Logger.LogMessage(Severity.Info, $"Flushing {initialCount} queued entries");
            progress?.Report(new QueueFlushProgress(initialCount, initialCount));

            // Drain in original FIFO order so snapshots remain behind the positions they represent.
            while (await ProcessNextAsync(cancellationToken).ConfigureAwait(false))
            {
                progress?.Report(new QueueFlushProgress(initialCount, _queue.Count));
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
            _queue.Dispose();
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

                    // Process one record at a time so cancellation can leave the remaining FIFO on disk.
                    while (!token.IsCancellationRequested && await ProcessNextAsync(token).ConfigureAwait(false))
                    {
                    }
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
        /// Processes an item from the queue.
        /// </summary>
        /// <param name="item"></param>
        /// <returns>Whether the database attempt succeeded.</returns>
        private async Task<bool> ProcessAsync(object item, CancellationToken cancellationToken)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (item is TrackedAircraft aircraft)
                {
                    await WriteTrackedAircraftAsync(aircraft, cancellationToken);
                }

                if (item is AircraftPosition position)
                {
                    await WriteAircraftPositionAsync(position, cancellationToken);
                }

                if (item is PositionDensitySnapshotEntity snapshot)
                {
                    await WritePositionDensitySnapshotAsync(snapshot, cancellationToken);
                }

                return true;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                // Log and sink the exception. The writer needs to continue or the application will
                // stop writing to the database
                _factory.Logger.LogException(ex);
                return false;
            }
        }

        /// <summary>
        /// Processes and acknowledges the next queued record.
        /// </summary>
        /// <returns>Whether a record was available.</returns>
        private async Task<bool> ProcessNextAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ISpoolQueueItem queued;
            try
            {
                queued = _queue.TryDequeue();
            }
            catch (InvalidDataException ex)
            {
                // The queue manager has discarded the unreadable head record so the FIFO can continue.
                _factory.Logger.LogMessage(Severity.Error, ex.Message);
                _factory.Logger.LogException(ex);
                return true;
            }

            if (queued is null)
            {
                return false;
            }

            using (queued)
            {
                var record = queued.Record;
                var succeeded = await ProcessAsync(GetEntity(record), cancellationToken).ConfigureAwait(false);

                // A completed database attempt is removed even when it failed; disposal without completion
                // is reserved for interruption before the attempt reaches an outcome.
                queued.Complete();

                if (!succeeded)
                {
                    _factory.Logger.LogMessage(
                        Severity.Warning,
                        $"Discarded failed spool record {record.Id} ({record.EntityType})");
                }
            }

            return true;
        }

        /// <summary>
        /// Returns the entity selected by a spool record discriminator.
        /// </summary>
        /// <param name="record">Persisted spool record.</param>
        /// <returns>Entity to write.</returns>
        private static object GetEntity(SpoolQueueRecord record)
            => record.EntityType switch
            {
                SpoolEntityType.TrackedAircraft => record.TrackedAircraft!,
                SpoolEntityType.AircraftPosition => record.AircraftPosition!,
                SpoolEntityType.PositionDensitySnapshot => record.PositionDensitySnapshot!,
                _ => throw new InvalidDataException($"Unsupported spool entity type: {record.EntityType}.")
            };

        /// <summary>
        /// Write a queued aircraft to the database
        /// </summary>
        /// <param name="aircraft"></param>
        /// <returns></returns>
        private async Task<bool> WriteTrackedAircraftAsync(TrackedAircraft aircraft, CancellationToken cancellationToken)
        {
            if (aircraft.SessionId is not > 0)
            {
                throw new InvalidOperationException(
                    $"Aircraft {aircraft.Address} cannot be persisted without an observation session.");
            }

            // See if it corresponds to an existing tracked aircraft record and, if so, set the aircraft
            // ID so that record will be updated rather than a new one created
            var activeAircraft = await _factory.AircraftLockManager.GetActiveAircraftAsync(
                aircraft.Address,
                aircraft.SessionId.Value,
                cancellationToken);
            if (activeAircraft != null)
            {
                aircraft.Id = activeAircraft.Id;
            }

            // Write the tracked aircraft
            _factory.Logger.LogMessage(Severity.Verbose, $"Writing aircraft {aircraft.Address} with Id {aircraft.Id}");
            await _factory.TrackedAircraftWriter.WriteAsync(aircraft, cancellationToken);

            return true;
        }

        /// <summary>
        /// Write a queued aircraft position to the database
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        private async Task<bool> WriteAircraftPositionAsync(AircraftPosition position, CancellationToken cancellationToken)
        {
            if (position.SessionId is not > 0)
            {
                throw new InvalidOperationException(
                    $"Position for aircraft {position.Address} cannot be persisted without an observation session.");
            }

            // Find the associated tracked aircraft. Aircraft are queued before their associated positions
            // and as it's a FIFO queue this should always return a valid aircraft. If the aircraft isn't
            // found, ignore the position record
            var activeAircraft = await _factory.AircraftLockManager.GetActiveAircraftAsync(
                position.Address,
                position.SessionId.Value,
                cancellationToken);
            if (activeAircraft == null)
            {
                return true;
            }

            // Assign the aircraft ID, for the foreign key relationship, and write the position
            position.AircraftId = activeAircraft.Id;
            await _factory.PositionWriter.WriteAsync(position, cancellationToken);

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
        private async Task<bool> WritePositionDensitySnapshotAsync(PositionDensitySnapshotEntity snapshot, CancellationToken cancellationToken)
        {
            // The snapshot manager owns the transaction spanning the header and every populated cell.
            var snapshotId = await _factory.PositionDensitySnapshotManager.AddAsync(snapshot, cancellationToken);
            _factory.Logger.LogMessage(
                Severity.Info,
                $"Persisted position-density snapshot {snapshotId} for session {snapshot.SessionId} with {snapshot.Cells.Count} cells");
            return true;
        }

    }
}

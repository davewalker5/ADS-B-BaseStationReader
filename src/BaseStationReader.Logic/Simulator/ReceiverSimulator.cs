using BaseStationReader.Entities.Interfaces;
using BaseStationReader.Entities.Logging;
using BaseStationReader.Entities.Tracking;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace BaseStationReader.Logic.Simulator
{
    [ExcludeFromCodeCoverage]
    public class ReceiverSimulator : IReceiverSimulator, IDisposable
    {
        private readonly object _lock = new();

        private readonly TcpListener _listener;
        private readonly List<TcpClient> _clients = new();

        private readonly Random _random = new();
        private readonly List<Aircraft> _aircraft = new();

        private readonly ITrackerLogger _logger;
        private readonly ITrackerTimer _timer;
        private readonly IAircraftGenerator _aircraftGenerator;
        private readonly IMessageGenerator _messageGenerator;

        private readonly int _lifespan;
        private readonly int _numberOfAircraft;
        private bool _listening = false;

        public ReceiverSimulator(
            ITrackerLogger logger,
            ITrackerTimer timer,
            IAircraftGenerator aircraftGenerator,
            IMessageGenerator generator,
            int port,
            int lifespan,
            int numberOfAircraft)
        {
            _listener = new TcpListener(IPAddress.Loopback, port);
            _logger = logger;
            _timer = timer;
            _aircraftGenerator = aircraftGenerator;
            _messageGenerator = generator;
            _lifespan = lifespan;
            _numberOfAircraft = numberOfAircraft;
            _timer.Tick += OnTimer;
        }

        /// <summary>
        /// Start the simulator
        /// </summary>
        public async Task Start()
        {
            // Check the listener isn't running
            if (!_listening)
            {
                _logger.LogMessage(Severity.Info, "Starting listener");

                // Start the timer
                _timer.Start();

                // Start the listener and listen for incoming connections until the listener is stopped
                _listening = true;
                _listener.Start();
                while (_listening)
                {
                    // Listen for the next connection and add the client to the collection
                    var client = await _listener.AcceptTcpClientAsync();
                    lock (_lock)
                    {
                        _logger.LogMessage(Severity.Info, "New client connected");
                        _clients.Add(client);
                    }
                }

                _logger.LogMessage(Severity.Info, "Exited listener connection loop");
            }
        }

        /// <summary>
        /// Stop the simulator
        /// </summary>
        public void Stop()
        {
            // Check the listener is running
            if (_listening)
            {
                // Stop the timer
                _timer.Stop();

                // Stop the listener
                _logger.LogMessage(Severity.Info, "Stopping listener");
                _listening = false;
                _listener.Stop();

                // Close and dispose the clients
                _logger.LogMessage(Severity.Info, "Disposing connected clients");
                foreach (var client in _clients)
                {
                    client.Close();
                    client.Dispose();
                }

                // Clear the client list
                _clients.Clear();
            }
        }

        /// <summary>
        /// Add new aircraft up to the maximum specified
        /// </summary>
        private void TopUpAircraft()
        {
            // Capture a list of existing addresses. The generator uses these to avoid duplicates
            var existingAddresses = _aircraft.Select(x => x.Address).ToList();

            // Iterate until the aircraft list is fully populated
            while (_aircraft.Count < _numberOfAircraft)
            {
                // Generate a new aircraft and add it's address to the "existing" list
                var generated = _aircraftGenerator.Generate(existingAddresses);
                existingAddresses.Add(generated.Address);

                // Add the generated aircraft to the active aircraft collection
                _aircraft.Add(generated);
            }
        }

        /// <summary>
        /// Remove aircraft that have been in the list for longer than the aircraft lifespan
        /// </summary>
        private void RemoveExpiredAircraft()
        {
            // Determine the cutoff date and time
            var cutoff = DateTime.Now.AddMilliseconds(-_lifespan);

            // Compile a list of aircraft to be removed
            var expired = _aircraft.Where(x => x.FirstSeen < cutoff).Select(x => x.Address);
            if (expired.Count() > 0)
            {
                // Log the removal and remove them
                var addresses = string.Join(',', expired);
                _logger.LogMessage(Severity.Info, $"Removing aircraft {addresses}");
                _aircraft.RemoveAll(x => expired.Contains(x.Address));
            }
        }

        /// <summary>
        /// Handler to handle timer events
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnTimer(object? sender, EventArgs e)
        {
            _timer.Stop();

            lock (_lock)
            {
                // Remove expired aircraft
                RemoveExpiredAircraft();

                // Top the aircraft list up to the required number
                TopUpAircraft();

                // Generate the message
                var message = GenerateMessage();

                // Send the message to each client
                BroadcastMessage(message);
            }

            _timer.Start();
        }

        /// <summary>
        /// Generate the next message, from a randomly selected aircraft
        /// </summary>
        /// <returns></returns>
        private byte[] GenerateMessage()
        {
            // Select the aircraft
            var index = _random.Next(0, _aircraft.Count);
            var aircraft = _aircraft[index];

            /// Create the message instance
            var message = _messageGenerator.Generate(aircraft.Address, aircraft.Callsign, aircraft.Squawk);

            // Log it in Base Station format
            var basestation = message.ToBaseStation();
            _logger.LogMessage(Severity.Info, basestation);

            // Generate a byte array representing the message in BaseStation format
            var messageBytes = Encoding.UTF8.GetBytes($"{basestation}\r\n");

            return messageBytes;
        }

        /// <summary>
        /// Send the specified message to all clients
        /// </summary>
        /// <param name="message"></param>
        private void BroadcastMessage(byte[] message)
        {
            // Create a list to capture clients where sending the message causes an exception
            var errored = new List<TcpClient>();

            // Iterate over each client, sending the message to each one in turn
            foreach (var client in _clients)
            {
                try
                {
                    client.GetStream().Write(message);
                }
                catch
                {
                    // Got an error sending to the client, so add it to the erroring list for removal
                    errored.Add(client);
                }
            }

            // Remove any clients that caused an error in the send attempt
            _clients.RemoveAll(x => errored.Contains(x));
        }

        /// <summary>
        /// IDisposable implementation
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// IDisposable implementation
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                Stop();
            }
        }
    }
}

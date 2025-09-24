using BaseStationReader.BusinessLogic.Geometry;
using BaseStationReader.Entities.Interfaces;
using BaseStationReader.Entities.Logging;
using BaseStationReader.Entities.Tracking;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace BaseStationReader.BusinessLogic.Simulator
{
    [ExcludeFromCodeCoverage]
    public class ReceiverSimulator : IReceiverSimulator, IDisposable
    {
        private readonly object _lock = new();

        private readonly TcpListener _listener;
        private readonly List<TcpClient> _clients = new();

        private readonly Random _random = new();
        private readonly List<TrackedAircraft> _aircraft = new();

        private readonly ITrackerLogger _logger;
        private readonly ITrackerTimer _timer;
        private readonly IAircraftGenerator _aircraftGenerator;
        private readonly IMessageGeneratorWrapper _messageGeneratorWrapper;

        private readonly int _maximumAltitude;
        private readonly int _numberOfAircraft;
        private bool _listening = false;

        public ReceiverSimulator(
            ITrackerLogger logger,
            ITrackerTimer timer,
            IAircraftGenerator aircraftGenerator,
            IMessageGeneratorWrapper generatorWrapper,
            int maximumAltitude,
            int port,
            int numberOfAircraft)
        {
            _listener = new TcpListener(IPAddress.Loopback, port);
            _logger = logger;
            _timer = timer;
            _aircraftGenerator = aircraftGenerator;
            _messageGeneratorWrapper = generatorWrapper;
            _maximumAltitude = maximumAltitude;
            _numberOfAircraft = numberOfAircraft;
            _timer.Tick += OnTimer;
        }

        /// <summary>
        /// Start the simulator
        /// </summary>
        public async Task StartAsync()
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
            // Compile a list of aircraft to be removed
            var expired = _aircraft.Where(x => x.FirstSeen < DateTime.Now.AddMilliseconds(-x.Lifespan)).Select(x => x.Address);
            if (expired.Any())
            {
                // Log the removal and remove them
                var addresses = string.Join(',', expired);
                _logger.LogMessage(Severity.Info, $"Removing aircraft {addresses}");
                _aircraft.RemoveAll(x => expired.Contains(x.Address));
            }
        }

        /// <summary>
        /// Update the positions of the aircraft
        /// </summary>
        private void UpdateAircraftPositions()
        {
            // Compile a list of aircraft that are still moving and need a position update
            var now = DateTime.Now;
            var aircraft = _aircraft.Where(x =>
                ((now - x.PositionLastUpdated).TotalMilliseconds >= 1000) &&
                (x.GroundSpeed > 0));

            // Update the positions for those aircraft
            foreach (var a in aircraft)
            {
                // Calculate the updated position
                (double latitude, double longitude) = CoordinateMathematics.DestinationPoint(
                    (double)a.Latitude.Value,
                    (double)a.Longitude.Value,
                    (double)a.Track.Value,
                    (double)a.GroundSpeed.Value);

                // Set the new position and altitude, clipping the altitude to the range 0 to
                // the maximum configured altitude
                a.PositionLastUpdated = now;
                a.Latitude = (decimal)latitude;
                a.Longitude = (decimal)longitude;
                a.Altitude = Math.Max(0, a.Altitude.Value + a.VerticalRate.Value);
                a.Altitude = Math.Min(a.Altitude.Value, _maximumAltitude);

                // If the altitude has reached zero, stop the aircraft from moving
                if (a.Altitude == 0)
                {
                    a.GroundSpeed = 0;
                }

                // Generate and broadcast a surface position message for this aircraft
                var message = GenerateMessage(a, "SurfacePosition");
                BroadcastMessage(message);
            }
        }

        /// <summary>
        /// Handler to handle timer events
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnTimer(object sender, EventArgs e)
        {
            _timer.Stop();

            lock (_lock)
            {
                // Remove expired aircraft
                RemoveExpiredAircraft();

                // Update aircraft positions
                UpdateAircraftPositions();

                // Top the aircraft list up to the required number
                TopUpAircraft();

                // The position updates will automatically generate position updates but also generate
                // another random message
                var message = GenerateMessage();

                // Send the message to each client
                BroadcastMessage(message);
            }

            _timer.Start();
        }

        /// <summary>
        /// Generate the next message, from a randomly selected aircraft
        /// </summary>
        /// <param name="messageType"></param>
        /// <returns></returns>
        private byte[] GenerateMessage(string messageType = "")
        {
            // Select the aircraft
            var index = _random.Next(0, _aircraft.Count);
            var aircraft = _aircraft[index];

            // Generate the message
            var messageBytes = GenerateMessage(aircraft, messageType);

            return messageBytes;
        }

        /// <summary>
        /// Generate a message from a specified aircraft
        /// </summary>
        /// <param name="aircraft"></param>
        /// <param name="messageType"></param>
        /// <returns></returns>
        private byte[] GenerateMessage(TrackedAircraft aircraft, string messageType = "")
        {
            /// Create the message instance
            var message = !string.IsNullOrEmpty(messageType) ?
                _messageGeneratorWrapper.Generate(aircraft, messageType) :
                _messageGeneratorWrapper.Generate(aircraft);

            // Log it in Base Station format
            var basestation = message.ToBaseStation();
            _logger.LogMessage(Severity.Debug, basestation);

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

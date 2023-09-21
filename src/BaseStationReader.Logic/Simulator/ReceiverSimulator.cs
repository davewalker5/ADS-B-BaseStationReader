using BaseStationReader.Entities.Interfaces;
using BaseStationReader.Entities.Logging;
using BaseStationReader.Entities.Tracking;
using System.Diagnostics.CodeAnalysis;

namespace BaseStationReader.Logic.Simulator
{
    [ExcludeFromCodeCoverage]
    public class ReceiverSimulator : IReceiverSimulator
    {
        private readonly Random _random = new();
        private readonly List<Aircraft> _aircraft = new();
        private readonly ITrackerLogger _logger;
        private readonly ITrackerTimer _timer;
        private readonly IAircraftGenerator _aircraftGenerator;
        private readonly IMessageGenerator _messageGenerator;

        private readonly int _port;
        private readonly int _lifespan;
        private readonly int _numberOfAircraft;

        public ReceiverSimulator(
            ITrackerLogger logger,
            ITrackerTimer timer,
            IAircraftGenerator aircraftGenerator,
            IMessageGenerator generator,
            int port,
            int lifespan,
            int numberOfAircraft)
        {
            _logger = logger;
            _timer = timer;
            _aircraftGenerator = aircraftGenerator;
            _messageGenerator = generator;
            _port = port;
            _lifespan = lifespan;
            _numberOfAircraft = numberOfAircraft;
            _timer.Tick += OnTimer;
        }

        /// <summary>
        /// Start the simulator
        /// </summary>
        public void Start()
        {
            _logger.LogMessage(Severity.Info, "Starting simulator");

            // Top up the aircraft list to the required number
            TopUpAircraft();

            // Start the timer
            _timer.Start();
        }

        /// <summary>
        /// Stop the simulator
        /// </summary>
        public void Stop()
        {
            _logger.LogMessage(Severity.Info, "Stopping simulator");
            _timer.Stop();
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

            // Remove expired aircraft
            RemoveExpiredAircraft();

            // Top the aircraft list up to the required number
            TopUpAircraft();

            // Generate the next message, from a randomly selected aircraft
            var index = _random.Next(0, _aircraft.Count);
            var aircraft = _aircraft[index];
            var message = _messageGenerator.Generate(aircraft.Address, aircraft.Callsign, aircraft.Squawk);

            // TODO : Send the message

            _timer.Start();
        }
    }
}

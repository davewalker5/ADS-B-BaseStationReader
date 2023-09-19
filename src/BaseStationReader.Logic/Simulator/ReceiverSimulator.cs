using BaseStationReader.Entities.Interfaces;
using BaseStationReader.Entities.Logging;
using BaseStationReader.Entities.Tracking;

namespace BaseStationReader.Logic.Simulator
{
    public class ReceiverSimulator : IReceiverSimulator
    {
        private readonly Random _random = new();
        private readonly List<Aircraft> _aircraft = new();
        private readonly ITrackerLogger _logger;
        private readonly ITrackerTimer _timer;
        private readonly int _port;
        private readonly int _lifespan;
        private readonly int _numberOfAircraft;

        public ReceiverSimulator(ITrackerLogger logger, ITrackerTimer timer, int port, int lifespan, int numberOfAircraft)
        {
            _logger = logger;
            _port = port;
            _lifespan = lifespan;
            _numberOfAircraft = numberOfAircraft;
            _timer = timer;
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
        /// Create a new aircraft and add it to the collection
        /// </summary>
        /// <returns></returns>
        private void AddAircraft()
        {
            Aircraft? aircraft = null;
            string address;

            do
            {
                // Create a new random address and make sure it's not already in the list
                address = _random.Next(0, 16777215).ToString("X6");
                if (!_aircraft.Select(x => x.Address).Contains(address))
                {
                    // Not in the list so create a new aircraft using the address
                    aircraft = new Aircraft
                    {
                        Address = address,
                        FirstSeen = DateTime.Now,
                        LastSeen = DateTime.Now
                    };
                }
            }
            while (aircraft == null);

            // Add the newly created aircraft to the list and log it
            _logger.LogMessage(Severity.Info, $"Created aircraft {address}");
            _aircraft.Add(aircraft);
        }

        /// <summary>
        /// Add new aircraft up to the maximum specified
        /// </summary>
        private void TopUpAircraft()
        {
            while (_aircraft.Count < _numberOfAircraft)
            {
                AddAircraft();
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

            // TODO : Send the next message

            _timer.Start();
        }
    }
}

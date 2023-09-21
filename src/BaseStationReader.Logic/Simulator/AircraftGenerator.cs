using BaseStationReader.Entities.Interfaces;
using BaseStationReader.Entities.Logging;
using BaseStationReader.Entities.Tracking;

namespace BaseStationReader.Logic.Simulator
{
    public class AircraftGenerator : IAircraftGenerator
    {
        private readonly Random _random = new();
        private readonly ITrackerLogger _logger;

        public AircraftGenerator(ITrackerLogger logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Generate an aircraft with random properties
        /// </summary>
        /// <param name="existingAddresses"></param>
        /// <returns></returns>
        public Aircraft Generate(IEnumerable<string>? existingAddresses)
        {
            Aircraft? aircraft = null;
            string address;

            do
            {
                // Create a new random address and make sure it's not already in the list
                address = _random.Next(0, 16777215).ToString("X6");
                var existing = (existingAddresses != null) && existingAddresses.Contains(address);
                if (!existing)
                {
                    // Not in the list so create a new aircraft using the address
                    aircraft = new Aircraft
                    {
                        Address = address,
                        Squawk = _random.Next(0, 9999).ToString("0000"),
                        FirstSeen = DateTime.Now,
                        LastSeen = DateTime.Now
                    };
                }
            }
            while (aircraft == null);

            // Log and return the aircraft
            _logger.LogMessage(Severity.Info, $"Created aircraft {address}");
            return aircraft;
        }
    }
}

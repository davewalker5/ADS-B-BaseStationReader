using BaseStationReader.Entities.Interfaces;
using BaseStationReader.Entities.Logging;
using BaseStationReader.Entities.Tracking;
using System;
using System.Text;

namespace BaseStationReader.Logic.Simulator
{
    public class AircraftGenerator : IAircraftGenerator
    {
        private const string LETTERS = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

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
        public Aircraft Generate(IEnumerable<string> existingAddresses)
        {
            Aircraft aircraft = null;
            string address;

            do
            {
                // Create a new random address and make sure it's not already in the list
                address = GenerateAddress();
                var existing = (existingAddresses != null) && existingAddresses.Contains(address);
                if (!existing)
                {
                    // Not in the list so create a new aircraft using the address
                    aircraft = new Aircraft
                    {
                        Address = address,
                        Callsign = GenerateCallsign(),
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

        /// <summary>
        /// Generate a random ICAO Address
        /// </summary>
        /// <returns></returns>
        private string GenerateAddress()
        {
            return _random.Next(0, 16777215).ToString("X6");
        }

        /// <summary>
        /// Generate a random callsign
        /// </summary>
        /// <returns></returns>
        private string GenerateCallsign()
        {
            StringBuilder builder = new StringBuilder();
            char letter;

            // Generate a 3-letter airline prefix
            for (int i = 0; i < 3; i++)
            {
                letter = LETTERS[_random.Next(0, LETTERS.Length)];
                builder.Append(letter);
            }

            // Add a rnadom flight number
            var number = _random.Next(1, 1000);
            builder.Append(number.ToString("000"));

            // Finally, add a trailing letter
            letter = LETTERS[_random.Next(0, LETTERS.Length)];
            builder.Append(letter);

            return builder.ToString();
        }
    }
}

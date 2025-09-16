using BaseStationReader.Entities.Config;
using BaseStationReader.Entities.Interfaces;
using BaseStationReader.Entities.Logging;
using BaseStationReader.Entities.Tracking;
using System.Text;

namespace BaseStationReader.BusinessLogic.Simulator
{
    public class AircraftGenerator : IAircraftGenerator
    {
        private const string LETTERS = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

        private readonly Random _random = new();
        private readonly ITrackerLogger _logger;
        private readonly SimulatorApplicationSettings _settings;

        public AircraftGenerator(ITrackerLogger logger, SimulatorApplicationSettings settings)
        {
            _logger = logger;
            _settings = settings;
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
                    // It's not, so create the aircraft instance
                    aircraft = new Aircraft
                    {
                        Address = address,
                    };
                }
            }
            while (aircraft == null);

            // Aircraft has a unique address, so now set the remaining propertis
            var flags = GenerateBehaviourFlags();
            aircraft.Callsign = GenerateCallsign();
            aircraft.Squawk = _random.Next(0, 9999).ToString("0000");
            aircraft.FirstSeen = DateTime.Now;
            aircraft.LastSeen = DateTime.Now;
            aircraft.SimulatorFlags = flags;
            aircraft.Track = _random.Next(0, 361);
            aircraft.GroundSpeed = GenerateGroundSpeed(flags);
            aircraft.VerticalRate = GenerateVerticalRate(flags);
            aircraft.Altitude = GenerateAltitude(flags, aircraft.VerticalRate.Value);

            // Calculate the initial position of the aircraft
            (decimal latitude, decimal longitude) = GenerateAircraftPostion(flags, aircraft.Track.Value, aircraft.GroundSpeed.Value);
            aircraft.Latitude = latitude;
            aircraft.Longitude = longitude;

            // Set the "position last updated" flag to now
            aircraft.PositionLastUpdated = DateTime.Now;

            // Log and return the aircraft
            _logger.LogMessage(Severity.Info, $"Created aircraft {address}");
            return aircraft;
        }

        /// <summary>
        /// Generate a random ICAO Address
        /// </summary>
        /// <returns></returns>
        private string GenerateAddress()
            => _random.Next(0, 16777215).ToString("X6");

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

        /// <summary>
        /// Generate a random flag indicating aircraft behaviour
        /// </summary>
        /// <returns></returns>
        private SimulatorFlags GenerateBehaviourFlags()
        {
            var selector = _random.Next(1, 4);
            return selector switch
            {
                1 => SimulatorFlags.LevelFlight,
                2 => SimulatorFlags.Landing,
                _ => SimulatorFlags.TakingOff,
            };
        }

        /// <summary>
        /// Generate the initial ground speed for an aircraft based on its intended behaviour
        /// </summary>
        /// <param name="flags"></param>
        /// <returns></returns>
        private decimal GenerateGroundSpeed(SimulatorFlags flags)
        {
            return flags switch
            {
                SimulatorFlags.Landing => _random.Next(_settings.MinimumApproachSpeed, _settings.MaximumApproachSpeed + 1),
                SimulatorFlags.TakingOff => _random.Next(_settings.MinimumTakeOffSpeed, _settings.MaximumTakeOffSpeed + 1),
                _ => _random.Next(_settings.MinimumCruisingSpeed, _settings.MaximumCruisingSpeed + 1)
            };
        }

        /// <summary>
        /// Generate a vertical rate between two limits
        /// </summary>
        /// <param name="minimum"></param>
        /// <param name="maximum"></param>
        /// <returns></returns>
        private decimal GenerateVerticalRate(decimal minimum, decimal maximum)
            => Math.Round((decimal)_random.NextDouble() * (maximum - minimum) + minimum, 1, MidpointRounding.AwayFromZero);

        /// <summary>
        /// Generate the initial vertical rate for an aircraft based on its intended behaviour
        /// </summary>
        /// <param name="flags"></param>
        /// <returns></returns>
        private decimal GenerateVerticalRate(SimulatorFlags flags)
        {
            return flags switch
            {
                SimulatorFlags.Landing => -GenerateVerticalRate(_settings.MinimumDescentRate, _settings.MaximumDescentRate),
                SimulatorFlags.TakingOff => GenerateVerticalRate(_settings.MinimumClimbRate, _settings.MaximumClimbRate),
                _ => 0M
            };
        }

        /// <summary>
        /// Generate the initial altitude for an aircraft based on its intended behaviour
        /// </summary>
        /// <returns></returns>
        private decimal GenerateAltitude(SimulatorFlags flags, decimal verticalRate)
        {
            // For aircraft that are landing, note that the lifespan is expressed in milliseconds
            return flags switch
            {
                SimulatorFlags.Landing => Math.Abs(verticalRate) * _settings.AircraftLifespan / 1000M,
                SimulatorFlags.TakingOff => 0M,
                _ => (decimal)_random.NextDouble() * (_settings.MaximumAltitude - _settings.MinimumAltitude) + _settings.MinimumAltitude,
            };
        }

        /// <summary>
        /// Calculate the initial position of the aircraft based on its behaviour
        /// </summary>
        /// <param name="flags"></param>
        /// <param name="heading"></param>
        /// <param name="speed"></param>
        /// <returns></returns>
        private (decimal latitude, decimal longitude) GenerateAircraftPostion(
            SimulatorFlags flags,
            decimal heading,
            decimal speed)
        {
            double latitude;
            double longitude;

            switch (flags)
            {
                case SimulatorFlags.TakingOff:
                    // Take off from the receiver position
                    latitude = _settings.ReceiverLatitude;
                    longitude = _settings.ReceiverLongitude;
                    break;
                case SimulatorFlags.Landing:
                    // Use the heading, speed and aircraft lifespan to calculate an initial position that will
                    // result in a landing at the receiver as the aircraft expires. Note that aircraft lifespan
                    // in the settings file is expressed in milliseconds
                    (latitude, longitude) = CoordinateMathematics.GenerateInboundAircraftPosition(
                        _settings.ReceiverLatitude,
                        _settings.ReceiverLongitude,
                        (double)heading,
                        (double)speed,
                        _settings.AircraftLifespan / 1000.0);
                    break;
                default:
                    // For level flight, generate a random position in a circle centred on the receiver
                    (latitude, longitude) = CoordinateMathematics.GenerateRandomStartingPosition(
                        _settings.ReceiverLatitude,
                        _settings.ReceiverLongitude,
                        _settings.MaximumInitialRange);
                    break;
            }

            return ((decimal)latitude, (decimal)longitude);
        }
    }
}

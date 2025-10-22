using BaseStationReader.BusinessLogic.Geometry;
using BaseStationReader.Entities.Config;
using BaseStationReader.Interfaces.Simulator;
using BaseStationReader.Entities.Logging;
using BaseStationReader.Entities.Tracking;
using System.Text;
using System.Text.RegularExpressions;
using BaseStationReader.Interfaces.Logging;
using System.Diagnostics.CodeAnalysis;

namespace BaseStationReader.BusinessLogic.Simulator
{
    public class AircraftGenerator : IAircraftGenerator
    {
        private const string LETTERS = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

        private readonly Random _random = new();
        private readonly ITrackerLogger _logger;
        private readonly SimulatorApplicationSettings _settings;
        private readonly List<string> _aircraftAddresses;
        private int _nextAddress = 0;

        public AircraftGenerator(
            ITrackerLogger logger,
            SimulatorApplicationSettings settings,
            IEnumerable<string> aircraftAddresses)
        {
            _logger = logger;
            _settings = settings;
            _aircraftAddresses = CuratedAddressList(aircraftAddresses);
        }

        /// <summary>
        /// Generate an aircraft with random properties
        /// </summary>
        /// <param name="existingAddresses"></param>
        /// <returns></returns>
        public TrackedAircraft Generate(IEnumerable<string> existingAddresses)
        {
            TrackedAircraft aircraft = null;

            // Select the next address from the address list that is not currently in use
            string address = SelectAddress(existingAddresses);

            // If all of the supplied addresses are in use (or there are none), generate
            // a random one
            address ??= GenerateAddress(existingAddresses);

            // Create the aircraft instance
            aircraft = new TrackedAircraft
            {
                Address = address,
            };

            // Aircraft has a unique address, so now set the remaining propertis
            var flags = GenerateAircraftBehaviour();
            aircraft.Callsign = GenerateCallsign();
            aircraft.Squawk = _random.Next(0, 9999).ToString("0000");
            aircraft.FirstSeen = DateTime.Now;
            aircraft.LastSeen = DateTime.Now;
            aircraft.Behaviour = flags;
            aircraft.Track = _random.Next(0, 361);
            aircraft.Lifespan = _random.Next(_settings.MinimumAircraftLifespan, _settings.MaximumAircraftLifespan + 1);
            aircraft.GroundSpeed = GenerateGroundSpeed(flags);
            aircraft.VerticalRate = GenerateVerticalRate(flags);
            aircraft.Altitude = GenerateAltitude(flags, aircraft.VerticalRate.Value, aircraft.Lifespan);

            // Calculate the initial position of the aircraft
            (decimal latitude, decimal longitude) = GenerateAircraftPosition(
                flags, aircraft.Track.Value, aircraft.GroundSpeed.Value, aircraft.Lifespan);
            aircraft.Latitude = latitude;
            aircraft.Longitude = longitude;

            // Set the "position last updated" flag to now
            aircraft.PositionLastUpdated = DateTime.Now;

            // Log and return the aircraft
            _logger.LogMessage(Severity.Info, $"Created aircraft {address}, lifespan {aircraft.Lifespan} ms, behaviour = {aircraft.Behaviour}");
            return aircraft;
        }

        /// <summary>
        /// Curate the list of addresses to ensure all entries are valid
        /// </summary>
        /// <param name="addresses"></param>
        /// <returns></returns>
        internal static List<string> CuratedAddressList(IEnumerable<string> addresses)
            => addresses?
                .Where(x => Regex.IsMatch(x, @"^[A-Za-z0-9]{6}$"))
                .Select(x => x.ToUpper())
                .ToList();

        /// <summary>
        /// Generate a random address
        /// </summary>
        /// <param name="existingAddresses"></param>
        /// <returns></returns>
        [ExcludeFromCodeCoverage]
        private string GenerateAddress(IEnumerable<string> existingAddresses)
        {
            string address = null;

            do
            {
                // Create a new random address
                address = GenerateRandomAddress();

                // See if it's in the existing address list
                var existing = (existingAddresses != null) && existingAddresses.Contains(address);
                if (existing)
                {
                    // It is, so clear it and try again
                    address = null;
                }
            }
            while (address == null);

            return address;
        }

        /// <summary>
        /// Select the next address from the address list that is not currently in use
        /// </summary>
        /// <returns></returns>
        internal string SelectAddress(IEnumerable<string> existingAddresses)
        {
            string address = null;

            // Check we have some addresses
            if (_aircraftAddresses?.Count > 0)
            {
                // Capture the initial "next address" index
                var initialIndex = _nextAddress;

                do
                {
                    // Select the next address
                    address = _aircraftAddresses[_nextAddress];

                    // Increment the count and wrap round to the start if necessary
                    _nextAddress = _nextAddress == (_aircraftAddresses.Count - 1) ? 0 : _nextAddress + 1;

                    // Check it's not in the existing list. If it is, try again
                    if (existingAddresses?.Contains(address) == true)
                    {
                        address = null;
                    }
                }
                while ((address == null) && (_nextAddress != initialIndex));
            }

            return address;
        }

        /// <summary>
        /// Generate a random ICAO Address
        /// </summary>
        /// <returns></returns>
        private string GenerateRandomAddress()
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

            // Add a random flight IATA code
            var number = _random.Next(1, 1000);
            builder.Append(number.ToString("000"));

            // Finally, add a trailing letter
            letter = LETTERS[_random.Next(0, LETTERS.Length)];
            builder.Append(letter);

            return builder.ToString();
        }

        /// <summary>
        /// Generate a random value indicating required aircraft behaviour
        /// </summary>
        /// <returns></returns>
        private AircraftBehaviour GenerateAircraftBehaviour()
        {
            var selector = _random.Next(1, 4);
            return selector switch
            {
                1 => AircraftBehaviour.LevelFlight,
                2 => AircraftBehaviour.Descending,
                _ => AircraftBehaviour.Climbing,
            };
        }

        /// <summary>
        /// Generate the initial ground speed for an aircraft based on its intended behaviour
        /// </summary>
        /// <param name="behaviour"></param>
        /// <returns></returns>
        private decimal GenerateGroundSpeed(AircraftBehaviour behaviour)
        {
            return behaviour switch
            {
                AircraftBehaviour.Descending => _random.Next(_settings.MinimumApproachSpeed, _settings.MaximumApproachSpeed + 1),
                AircraftBehaviour.Climbing => _random.Next(_settings.MinimumTakeOffSpeed, _settings.MaximumTakeOffSpeed + 1),
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
        /// <param name="behaviour"></param>
        /// <returns></returns>
        private decimal GenerateVerticalRate(AircraftBehaviour behaviour)
        {
            return behaviour switch
            {
                AircraftBehaviour.Descending => -GenerateVerticalRate(_settings.MinimumDescentRate, _settings.MaximumDescentRate),
                AircraftBehaviour.Climbing => GenerateVerticalRate(_settings.MinimumClimbRate, _settings.MaximumClimbRate),
                _ => 0M
            };
        }

        /// <summary>
        /// Generate the initial altitude for an aircraft based on its intended behaviour
        /// </summary>
        /// <param name="behaviour"></param>
        /// <param name="verticalRate"></param>
        /// <param name="lifespan"></param>
        /// <returns></returns>
        private decimal GenerateAltitude(AircraftBehaviour behaviour, decimal verticalRate, int lifespan)
        {
            // For aircraft that are landing, note that the lifespan is expressed in milliseconds
            return behaviour switch
            {
                AircraftBehaviour.Descending => Math.Abs(verticalRate) * lifespan / 1000M,
                AircraftBehaviour.Climbing => 0M,
                _ => (decimal)_random.NextDouble() * (_settings.MaximumAltitude - _settings.MinimumAltitude) + _settings.MinimumAltitude,
            };
        }

        /// <summary>
        /// Calculate the initial position of the aircraft based on its behaviour
        /// </summary>
        /// <param name="behaviour"></param>
        /// <param name="heading"></param>
        /// <param name="speed"></param>
        /// <returns></returns>
        private (decimal latitude, decimal longitude) GenerateAircraftPosition(
            AircraftBehaviour behaviour,
            decimal heading,
            decimal speed,
            int lifespan)
        {
            double latitude;
            double longitude;

            switch (behaviour)
            {
                case AircraftBehaviour.Climbing:
                    // Take off from the receiver position
                    latitude = _settings.ReceiverLatitude;
                    longitude = _settings.ReceiverLongitude;
                    break;
                case AircraftBehaviour.Descending:
                    // Use the heading, speed and aircraft lifespan to calculate an initial position that will
                    // result in a landing at the receiver as the aircraft expires. Note that aircraft lifespan
                    // in the settings file is expressed in milliseconds
                    (latitude, longitude) = CoordinateMathematics.GenerateInboundAircraftPosition(
                        _settings.ReceiverLatitude,
                        _settings.ReceiverLongitude,
                        (double)heading,
                        (double)speed,
                        lifespan / 1000.0);
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

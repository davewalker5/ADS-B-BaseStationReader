using BaseStationReader.BusinessLogic.Geometry;
using BaseStationReader.BusinessLogic.Simulator;
using BaseStationReader.BusinessLogic.Tracking;
using BaseStationReader.Entities.Interfaces;
using BaseStationReader.Entities.Tracking;
using BaseStationReader.Tests.Mocks;

namespace BaseStationReader.Tests
{
    [TestClass]
    public class AircraftPropertyUpdaterTest : SimulatorTestBase
    {
        private ITrackerLogger _logger;
        private IAircraftGenerator _aircraftGenerator;
        private readonly IDistanceCalculator _distanceCalculator = new HaversineCalculator();
        private readonly IAircraftBehaviourAssessor _behaviourAssessor = new SimpleAircraftBehaviourAssessor();

        [TestInitialize]
        public void Initialise()
        {
            // Configure the simulated aircraft generator
            _logger = new MockFileLogger();
            _aircraftGenerator = new AircraftGenerator(_logger, _settings, null);
        }

        [TestMethod]
        public void UpdateWithoutDistanceTest()
        {
            var updater = new AircraftPropertyUpdater(_logger, null, _behaviourAssessor);

            // Generate a simulated aircraft and capture the original properties
            var aircraft = _aircraftGenerator.Generate([]);
            var altitude = aircraft.Altitude;
            var distance = aircraft.Distance;

            // Calculate an updated altitude
            var updatedAltitude = altitude + 10M;
            var expectedAltitudeFeet = MetresToFeet(updatedAltitude.Value);

            // Simulate a messgae and update the aircraft properties
            aircraft.Altitude = updatedAltitude;
            var message = new SurveillanceAltMessageGenerator(_logger).Generate(aircraft);
            updater.UpdateProperties(aircraft, message);

            Assert.AreNotEqual(altitude, aircraft.Altitude);
            Assert.AreEqual(expectedAltitudeFeet, aircraft.Altitude);
            Assert.AreEqual(distance, aircraft.Distance);
        }

        [TestMethod]
        public void UpdateWithDistanceTest()
        {
            var updater = new AircraftPropertyUpdater(_logger, _distanceCalculator, _behaviourAssessor);

            // Generate a simulated aircraft and capture the original properties
            var aircraft = _aircraftGenerator.Generate([]);
            var altitude = MetresToFeet(aircraft.Altitude.Value);
            var distance = aircraft.Distance;

            // Calculate an updated latitude and longitude
            var updatedLatitude = (decimal)_settings.ReceiverLatitude + 0.1M;
            var updatedLongitude = (decimal)_settings.ReceiverLongitude + 0.1M;

            // Simulate a messgae and update the aircraft properties
            aircraft.Latitude = updatedLatitude;
            aircraft.Longitude = updatedLongitude;
            var message = new AirbornePositionMessageGenerator(_logger).Generate(aircraft);
            updater.UpdateProperties(aircraft, message);

            Assert.AreEqual(altitude, aircraft.Altitude);
            Assert.AreNotEqual(distance, aircraft.Distance);
            Assert.AreEqual(updatedLatitude, aircraft.Latitude);
            Assert.AreEqual(updatedLongitude, aircraft.Longitude);
        }

        [TestMethod]
        public void AssessClimbingBehaviourTest()
        {
            var updater = new AircraftPropertyUpdater(_logger, null, _behaviourAssessor);

            // Generate a simulated aircraft and capture the original properties
            var aircraft = _aircraftGenerator.Generate([]);
            aircraft.Behaviour = AircraftBehaviour.Unknown;

            // Set up a pattern of climbing behaviour
            var altitude = aircraft.Altitude;
            aircraft.AltitudeHistory.Add(20M);
            aircraft.AltitudeHistory.Add(-5M);
            aircraft.AltitudeHistory.Add(10M);
            aircraft.Altitude += 27M;

            // Assess the aircraft behaviour
            updater.UpdateBehaviour(aircraft, altitude);
            Assert.AreEqual(AircraftBehaviour.Climbing, aircraft.Behaviour);
        }

        [TestMethod]
        public void AssessDescendingBehaviourTest()
        {
            var updater = new AircraftPropertyUpdater(_logger, null, _behaviourAssessor);

            // Generate a simulated aircraft and capture the original properties
            var aircraft = _aircraftGenerator.Generate([]);
            aircraft.Behaviour = AircraftBehaviour.Unknown;

            // Set up a pattern of climbing behaviour
            var altitude = aircraft.Altitude;
            aircraft.AltitudeHistory.Add(-20M);
            aircraft.AltitudeHistory.Add(5M);
            aircraft.AltitudeHistory.Add(-10M);
            aircraft.Altitude -= 27M;

            // Assess the aircraft behaviour
            updater.UpdateBehaviour(aircraft, altitude);
            Assert.AreEqual(AircraftBehaviour.Descending, aircraft.Behaviour);
        }


        [TestMethod]
        public void AssessLevelFlightBehaviourTest()
        {
            var updater = new AircraftPropertyUpdater(_logger, null, _behaviourAssessor);

            // Generate a simulated aircraft and capture the original properties
            var aircraft = _aircraftGenerator.Generate([]);
            aircraft.Behaviour = AircraftBehaviour.Unknown;

            // Set up a pattern of level flight behaviour
            aircraft.AltitudeHistory.Add(0M);
            aircraft.AltitudeHistory.Add(1M);
            aircraft.AltitudeHistory.Add(0M);

            // Assess the aircraft behaviour
            updater.UpdateBehaviour(aircraft, aircraft.Altitude);
            Assert.AreEqual(AircraftBehaviour.LevelFlight, aircraft.Behaviour);
        }

        private static decimal MetresToFeet(decimal metres)
            => Math.Round(metres * 3.28084M, MidpointRounding.AwayFromZero);
    }
}
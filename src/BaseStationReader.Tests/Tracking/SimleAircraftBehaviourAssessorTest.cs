using BaseStationReader.BusinessLogic.Tracking;
using BaseStationReader.Entities.Tracking;

namespace BaseStationReader.Tests.Tracking
{
    [TestClass]
    public class SimleAircraftBehaviourAssessorTest
    {
        [TestMethod]
        public void InsufficientDataPointsTest()
        {
            var aircraft = new TrackedAircraft();
            aircraft.AltitudeHistory.Add(0M);
            aircraft.AltitudeHistory.Add(100M);
            var behaviour = new SimpleAircraftBehaviourAssessor().Assess(aircraft);
            Assert.AreEqual(AircraftBehaviour.Unknown, behaviour);
        }

        [TestMethod]
        public void LevelFlightTest()
        {
            var aircraft = new TrackedAircraft();
            aircraft.AltitudeHistory.Add(0M);
            aircraft.AltitudeHistory.Add(0M);
            aircraft.AltitudeHistory.Add(0M);
            var behaviour = new SimpleAircraftBehaviourAssessor().Assess(aircraft);
            Assert.AreEqual(AircraftBehaviour.LevelFlight, behaviour);
        }

        [TestMethod]
        public void ClimbingTest()
        {
            var aircraft = new TrackedAircraft();
            aircraft.AltitudeHistory.Add(10M);
            aircraft.AltitudeHistory.Add(-5M);
            aircraft.AltitudeHistory.Add(10M);
            aircraft.AltitudeHistory.Add(10M);
            var behaviour = new SimpleAircraftBehaviourAssessor().Assess(aircraft);
            Assert.AreEqual(AircraftBehaviour.Climbing, behaviour);
        }

        [TestMethod]
        public void DescendingTest()
        {
            var aircraft = new TrackedAircraft();
            aircraft.AltitudeHistory.Add(-10M);
            aircraft.AltitudeHistory.Add(5M);
            aircraft.AltitudeHistory.Add(-10M);
            aircraft.AltitudeHistory.Add(-10M);
            var behaviour = new SimpleAircraftBehaviourAssessor().Assess(aircraft);
            Assert.AreEqual(AircraftBehaviour.Descending, behaviour);
        }
    }
}
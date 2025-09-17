using System.Reflection;
using BaseStationReader.Entities.Interfaces;
using BaseStationReader.Entities.Logging;
using BaseStationReader.Entities.Messages;
using BaseStationReader.Entities.Tracking;

namespace BaseStationReader.BusinessLogic.Tracking
{
    public class AircraftPropertyUpdater : IAircraftPropertyUpdater
    {
        private readonly PropertyInfo[] _aircraftProperties = typeof(Aircraft).GetProperties(BindingFlags.Instance | BindingFlags.Public);
        private readonly PropertyInfo[] _messageProperties = typeof(Message).GetProperties(BindingFlags.Instance | BindingFlags.Public);

        private readonly ITrackerLogger _logger;
        private readonly IDistanceCalculator _distanceCalculator;
        private readonly IAircraftBehaviourAssessor _assessor;

        public AircraftPropertyUpdater(
            ITrackerLogger logger,
            IDistanceCalculator distanceCalculator,
            IAircraftBehaviourAssessor assessor)
        {
            _logger = logger;
            _distanceCalculator = distanceCalculator;
            _assessor = assessor;
        }

        /// <summary>
        /// Update an aircraft instance from a received message
        /// </summary>
        /// <param name="aircraft"></param>
        /// <param name="msg"></param>
        public void UpdateProperties(Aircraft aircraft, Message msg)
        {
            // Increment the message count
            aircraft.Messages++;

            // Capture the vertical rate before the aircraft is updated
            decimal? originalAltitude = aircraft.Altitude;

            // Iterate over the aircraft propertues
            foreach (var aircraftProperty in _aircraftProperties)
            {
                // Find the corresponding message property for the current aircraft property
                var messageProperty = Array.Find(_messageProperties, x => x.Name == aircraftProperty.Name);
                if (messageProperty != null)
                {
                    // See if the property has changed
                    var original = aircraftProperty.GetValue(aircraft);
                    var updated = messageProperty.GetValue(msg);
                    if (updated != null && original != updated)
                    {
                        // It has, so update it
                        aircraftProperty.SetValue(aircraft, updated);
                        aircraft.Status = TrackingStatus.Active;
                    }
                }
            }

            // If a distance calculator's been provided and we have an aircraft position, calculate the distance
            // from the reference position to the aircraft
            if ((_distanceCalculator != null) && (aircraft.Latitude != null) && (aircraft.Longitude != null))
            {
                var metres = _distanceCalculator.CalculateDistance((double)aircraft.Latitude, (double)aircraft.Longitude);
                aircraft.Distance = Math.Round(_distanceCalculator.MetresToNauticalMiles(metres), 0, MidpointRounding.AwayFromZero);
            }
        }

        /// <summary>
        /// Update the behaviour of an aircraft based on its altitude history
        /// </summary>
        /// <param name="aircraft"></param>
        /// <param name="lastAltitude"></param>
        public void UpdateBehaviour(Aircraft aircraft, decimal? lastAltitude)
        {
            // If the altitude is specified, use changes in altitude to characterise aircraft behaviour
            if ((lastAltitude != null) && (aircraft.Altitude != null))
            {
                // Calculate the change in altitude and add it to the history
                var altitudeChange = aircraft.Altitude.Value - lastAltitude.Value;
                aircraft.AltitudeHistory.Add(altitudeChange);

                // Assess the aircraft behaviour using the history
                aircraft.Behaviour = _assessor.Assess(aircraft);
                _logger.LogMessage(Severity.Debug, $"Aircraft {aircraft.Address} : {aircraft.Behaviour}");
            }
        }
    }
}
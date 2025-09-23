using BaseStationReader.Entities.Interfaces;
using BaseStationReader.Entities.Tracking;

namespace BaseStationReader.BusinessLogic.Tracking
{
    public class SimpleAircraftBehaviourAssessor : IAircraftBehaviourAssessor
    {
        /// <summary>
        /// Examine an aircrafts altitude history to determine if it's in level flight, climbing
        /// or descending
        /// </summary>
        /// <param name="aircraft"></param>
        /// <returns></returns>
        public AircraftBehaviour Assess(TrackedAircraft aircraft)
        {
            AircraftBehaviour behaviour = AircraftBehaviour.Unknown;

            // Get the history as a list and make sure it has sufficient entries for assessment
            var history = aircraft.AltitudeHistory.Items.ToList();
            if (history.Count > 2)
            {
                // Calculate the average change across all entries
                var averageChange = Math.Round(history.Average(x => x), 0, MidpointRounding.ToZero);

                // Assess the behaviour based on the average change
                if (averageChange < 0)
                {
                    behaviour = AircraftBehaviour.Descending;
                }
                else if (averageChange > 0)
                {
                    behaviour = AircraftBehaviour.Climbing;
                }
                else
                {
                    behaviour = AircraftBehaviour.LevelFlight;
                }
            }

            return behaviour;
        }
    }
}
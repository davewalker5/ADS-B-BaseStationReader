using BaseStationReader.Entities.Events;
using BaseStationReader.Entities.Logging;
using BaseStationReader.Entities.Tracking;
using BaseStationReader.Interfaces.Logging;
using BaseStationReader.Interfaces.Events;

namespace BaseStationReader.BusinessLogic.Events
{
    public class AircraftTrackerNotificationSender : AircraftNotificationSenderBase, IAircraftNotificationSender
    {
        private readonly ITrackerLogger _logger;
        private readonly int? _maximumDistance;
        private readonly int? _minimumAltitude;
        private readonly int? _maximumAltitude;
        private readonly IEnumerable<AircraftBehaviour> _behaviours;

        public AircraftTrackerNotificationSender(
            ITrackerLogger logger,
            IEnumerable<AircraftBehaviour> behaviours,
            int? maximumDistance,
            int? minimumAltitude,
            int? maximumAltitude) : base(logger)
        {
            _logger = logger;
            _maximumDistance = maximumDistance;
            _minimumAltitude = minimumAltitude;
            _maximumAltitude = maximumAltitude;
            _behaviours = behaviours;
        }

        /// <summary>
        /// Send an "aircraft event" notification to the subscribers to the specified handler
        /// </summary>
        /// <param name="aircraft"></param>
        /// <param name="previousPosition"></param>
        /// <param name="sender"></param>
        /// <param name="type"></param>
        /// <param name="handlers"></param>
        public override void SendAircraftNotification(
            TrackedAircraft aircraft,
            AircraftPosition previousPosition,
            object sender,
            AircraftNotificationType type,
            EventHandler<AircraftNotificationEventArgs> handlers)
        {
            var meetsTrackingCriteria = CheckTrackingCriteria(aircraft);
            Logger.LogMessage(Severity.Verbose,
                $"Sending {type} message for aircraft {aircraft.Address} {aircraft.Behaviour}");

            // Every detected aircraft must reach the controller for session persistence. The criteria flag lets
            // the controller keep position history and live tracking restricted to qualifying observations.
            NotifySubscribers(sender, handlers, new AircraftNotificationEventArgs
            {
                Aircraft = aircraft,
                Position = previousPosition,
                NotificationType = type,
                MeetsTrackingCriteria = meetsTrackingCriteria
            });
        }

        /// <summary>
        /// Return true if the behaviour of an aircraft matches the tracking criteria
        /// </summary>
        /// <param name="aircraft"></param>
        /// <returns></returns>
        private bool CheckBehaviourMatches(TrackedAircraft aircraft)
            => _behaviours.Contains(aircraft.Behaviour);

        /// <summary>
        /// Return true if an aircraft meets the criteria for notifications to be sent
        /// </summary>
        /// <param name="aircraft"></param>
        /// <returns></returns>
        private bool CheckTrackingCriteria(TrackedAircraft aircraft)
            => CheckBehaviourMatches(aircraft) &&
               ((_maximumDistance == null) || (aircraft.Distance <= _maximumDistance)) &&
               ((_minimumAltitude == null) || (aircraft.Altitude >= _minimumAltitude)) &&
               ((_maximumAltitude == null) || (aircraft.Altitude <= _maximumAltitude));
    }
}

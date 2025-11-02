
using BaseStationReader.Interfaces.Events;
using BaseStationReader.Interfaces.Logging;

namespace BaseStationReader.BusinessLogic.Events
{
    public class ControllerNotificationSender : AircraftNotificationSenderBase, IControllerNotificationSender
    {
        public ControllerNotificationSender(ITrackerLogger logger) : base(logger)
        {
        }
    }
}
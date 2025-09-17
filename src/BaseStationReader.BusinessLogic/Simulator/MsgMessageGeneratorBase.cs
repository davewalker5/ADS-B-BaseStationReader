using BaseStationReader.Entities.Interfaces;
using BaseStationReader.Entities.Logging;
using BaseStationReader.Entities.Messages;

namespace BaseStationReader.BusinessLogic.Simulator
{
    public abstract class MsgMessageGeneratorBase
    {
        private readonly ITrackerLogger _logger;

        protected MsgMessageGeneratorBase(ITrackerLogger logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Convert a descent or ascent rate expressed as m/s to ft/min
        /// </summary>
        /// <param name="rate"></param>
        /// <returns></returns>
        protected static decimal VerticalRateToFeetPerMinute(decimal rate)
            => Math.Round(rate * 196.85M, MidpointRounding.AwayFromZero);

        /// <summary>
        /// Convert an altitude in metres to feet
        /// </summary>
        /// <param name="metres"></param>
        /// <returns></returns>
        protected static decimal AltitudeToFeet(decimal metres)
            => Math.Round(metres * 3.28084M, MidpointRounding.AwayFromZero);

        /// <summary>
        /// Convert a ground speed in m/s to knots
        /// </summary>
        /// <param name="metresPerSecond"></param>
        /// <returns></returns>
        protected static decimal GroundSpeedToKnots(decimal metresPerSecond)
            => Math.Round(metresPerSecond * 1.943844M, MidpointRounding.AwayFromZero);

        /// <summary>
        /// Construct a message with the core fields common to all message types populated
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        protected static Message ConstructMessage(TransmissionType type, string address)
        {
            Message message = new()
            {
                MessageType = MessageType.MSG,
                TransmissionType = type,
                Address = address,
                Generated = DateTime.Now,
                LastSeen = DateTime.Now,
                Alert = false,
                Emergency = false,
                IsOnGround = false
            };

            return message;
        }

        /// <summary>
        /// Log a genrated message
        /// </summary>
        /// <param name="message"></param>
        protected void LogGeneratedMessage(Message message)
        {
            _logger.LogMessage(
                Severity.Debug,
                $"Generated MSG {(int)message.TransmissionType} ({message.TransmissionType.ToString()}) message");
        }
    }
}

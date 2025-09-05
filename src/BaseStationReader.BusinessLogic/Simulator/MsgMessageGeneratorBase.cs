using BaseStationReader.Entities.Interfaces;
using BaseStationReader.Entities.Logging;
using BaseStationReader.Entities.Messages;

namespace BaseStationReader.BusinessLogic.Simulator
{
    public abstract class MsgMessageGeneratorBase
    {
        protected readonly Random _random = new();
        private readonly ITrackerLogger _logger;

        protected MsgMessageGeneratorBase(ITrackerLogger logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Generate a random integer in the specified range (inclusive)
        /// </summary>
        /// <param name="minValue"></param>
        /// <param name="maxValue"></param>
        /// <returns></returns>
        protected int RandomInt(int minValue, int maxValue)
        {
            return _random.Next(minValue, maxValue + 1);
        }

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

using BaseStationReader.Entities.Interfaces;
using BaseStationReader.Entities.Messages;
using BaseStationReader.Entities.Tracking;

namespace BaseStationReader.BusinessLogic.Simulator
{
    public class AirborneVelocityMessageGenerator : MsgMessageGeneratorBase, IMessageGenerator
    {
        public AirborneVelocityMessageGenerator(ITrackerLogger logger) : base(logger)
        {

        }

        /// <summary>
        /// Generate a Airborne Velocity MSG message
        /// </summary>
        /// <param name="aircraft"></param>
        /// <returns></returns>
        public Message Generate(Aircraft aircraft)
        {
            // Generate the base message
            var message = ConstructMessage(TransmissionType.AirborneVelocity, aircraft.Address);

            // Populate the type-specific members. Note that the messages don't attempt to simulate a realistic route
            // for an aircraft over time. They're just randomly selected values for properties
            message.GroundSpeed = GroundSpeedToKnots(aircraft.GroundSpeed.Value);
            message.Track = aircraft.Track;
            message.VerticalRate = VerticalRateToFeetPerMinute(aircraft.VerticalRate.Value);

            // Log and return the message
            LogGeneratedMessage(message);
            return message;
        }
    }
}

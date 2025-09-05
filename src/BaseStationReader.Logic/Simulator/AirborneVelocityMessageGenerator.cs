using BaseStationReader.Entities.Interfaces;
using BaseStationReader.Entities.Messages;

namespace BaseStationReader.Logic.Simulator
{
    public class AirborneVelocityMessageGenerator : MsgMessageGeneratorBase, IMessageGenerator
    {
        public AirborneVelocityMessageGenerator(ITrackerLogger logger) : base(logger)
        {

        }

        /// <summary>
        /// Generate a Airborne Velocity MSG message
        /// </summary>
        /// <param name="address"></param>
        /// <param name="callsign"></param>
        /// <param name="squawk"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public Message Generate(string address, string callsign, string squawk)
        {
            // Generate the base message
            var message = ConstructMessage(TransmissionType.AirborneVelocity, address);

            // Populate the type-specific members. Note that the messages don't attempt to simulate a realistic route
            // for an aircraft over time. They're just randomly selected values for properties
            message.GroundSpeed = RandomInt(200, 475);
            message.Track = RandomInt(0, 360);
            message.VerticalRate = RandomInt(0, 1000);

            // Log and return the message
            LogGeneratedMessage(message);
            return message;
        }
    }
}

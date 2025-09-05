using BaseStationReader.Entities.Interfaces;
using BaseStationReader.Entities.Messages;

namespace BaseStationReader.BusinessLogic.Simulator
{
    public class SurveillanceAltMessageGenerator : MsgMessageGeneratorBase, IMessageGenerator
    {
        public SurveillanceAltMessageGenerator(ITrackerLogger logger) : base(logger)
        {

        }

        /// <summary>
        /// Generate a Surveillance Altitude MSG message
        /// </summary>
        /// <param name="address"></param>
        /// <param name="callsign"></param>
        /// <param name="squawk"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public Message Generate(string address, string callsign, string squawk)
        {
            // Generate the base message
            var message = ConstructMessage(TransmissionType.SurveillanceAlt, address);

            // Populate the type-specific members. Note that the messages don't attempt to simulate a realistic route
            // for an aircraft over time. They're just randomly selected values for properties
            message.Altitude = RandomInt(1000, 40000);

            // Log and return the message
            LogGeneratedMessage(message);
            return message;
        }
    }
}
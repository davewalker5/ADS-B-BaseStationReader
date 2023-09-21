using BaseStationReader.Entities.Interfaces;
using BaseStationReader.Entities.Messages;

namespace BaseStationReader.Logic.Simulator
{
    public class IdentificationMessageGenerator : MsgMessageGeneratorBase, IMessageGenerator
    {
        public IdentificationMessageGenerator(ITrackerLogger logger) : base(logger)
        {

        }

        /// <summary>
        /// Generate an Identification MSG message
        /// </summary>
        /// <param name="address"></param>
        /// <param name="callsign"></param>
        /// <param name="squawk"></param>
        /// <returns></returns>
        public Message Generate(string address, string? callsign, string? squawk)
        {
            // Generate the base message and populate the type-specific members
            var message = base.ConstructMessage(TransmissionType.Identification, address);
            message.Callsign = callsign;

            // Log and return the message
            LogGeneratedMessage(message);
            return message;
        }
    }
}

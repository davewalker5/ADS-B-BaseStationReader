using BaseStationReader.Entities.Interfaces;
using BaseStationReader.Entities.Messages;

namespace BaseStationReader.BusinessLogic.Simulator
{
    public class AllCallReplyMessageGenerator : MsgMessageGeneratorBase, IMessageGenerator
    {
        public AllCallReplyMessageGenerator(ITrackerLogger logger) : base(logger)
        {

        }

        /// <summary>
        /// Generate an Air to Air MSG message
        /// </summary>
        /// <param name="address"></param>
        /// <param name="callsign"></param>
        /// <param name="squawk"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public Message Generate(string address, string callsign, string squawk)
        {
            // Generate the base message - there are no further fields to populate for this message type
            var message = ConstructMessage(TransmissionType.AllCallReply, address);

            // Log and return the message
            LogGeneratedMessage(message);
            return message;
        }
    }
}
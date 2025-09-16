using BaseStationReader.Entities.Interfaces;
using BaseStationReader.Entities.Messages;
using BaseStationReader.Entities.Tracking;

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
        /// <param name="aircraft"></param>
        /// <returns></returns>
        public Message Generate(Aircraft aircraft)
        {
            // Generate the base message - there are no further fields to populate for this message type
            var message = ConstructMessage(TransmissionType.AllCallReply, aircraft.Address);

            // Log and return the message
            LogGeneratedMessage(message);
            return message;
        }
    }
}
using BaseStationReader.Entities.Interfaces;
using BaseStationReader.Entities.Messages;
using BaseStationReader.Entities.Tracking;

namespace BaseStationReader.BusinessLogic.Simulator
{
    public class IdentificationMessageGenerator : MsgMessageGeneratorBase, IMessageGenerator
    {
        public IdentificationMessageGenerator(ITrackerLogger logger) : base(logger)
        {

        }

        /// <summary>
        /// Generate an Identification MSG message
        /// </summary>
        /// <param name="aircraft"></param>
        /// <returns></returns>
        public Message Generate(TrackedAircraft aircraft)
        {
            // Generate the base message and populate the type-specific members
            var message = ConstructMessage(TransmissionType.Identification, aircraft.Address);
            message.Callsign = aircraft.Callsign;

            // Log and return the message
            LogGeneratedMessage(message);
            return message;
        }
    }
}

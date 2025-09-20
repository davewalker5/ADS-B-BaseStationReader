using BaseStationReader.Entities.Interfaces;
using BaseStationReader.Entities.Messages;
using BaseStationReader.Entities.Tracking;

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
        /// <param name="aircraft"></param>
        /// <returns></returns>
        public Message Generate(TrackedAircraft aircraft)
        {
            // Generate the base message
            var message = ConstructMessage(TransmissionType.SurveillanceAlt, aircraft.Address);

            // Populate the type-specific members. Note that the messages don't attempt to simulate a realistic route
            // for an aircraft over time. They're just randomly selected values for properties
            message.Altitude = AltitudeToFeet(aircraft.Altitude.Value);

            // Log and return the message
            LogGeneratedMessage(message);
            return message;
        }
    }
}
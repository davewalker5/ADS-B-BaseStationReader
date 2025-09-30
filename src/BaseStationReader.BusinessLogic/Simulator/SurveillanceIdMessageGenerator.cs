using BaseStationReader.Interfaces.Simulator;
using BaseStationReader.Entities.Messages;
using BaseStationReader.Entities.Tracking;
using BaseStationReader.Interfaces.Logging;

namespace BaseStationReader.BusinessLogic.Simulator
{
    public class SurveillanceIdMessageGenerator : MsgMessageGeneratorBase, IMessageGenerator
    {
        public SurveillanceIdMessageGenerator(ITrackerLogger logger) : base(logger)
        {

        }

        /// <summary>
        /// Generate a Surveillance Identification MSG message
        /// </summary>
        /// <param name="aircraft"></param>
        /// <returns></returns>
        public Message Generate(TrackedAircraft aircraft)
        {
            // Generate the base message
            var message = ConstructMessage(TransmissionType.SurveillanceId, aircraft.Address);

            // Populate the type-specific members. Note that the messages don't attempt to simulate a realistic route
            // for an aircraft over time. They're just randomly selected values for properties
            message.Altitude = AltitudeToFeet(aircraft.Altitude.Value);
            message.Squawk = aircraft.Squawk;

            // Log and return the message
            LogGeneratedMessage(message);
            return message;
        }
    }
}
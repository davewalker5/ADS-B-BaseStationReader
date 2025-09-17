using BaseStationReader.Entities.Interfaces;
using BaseStationReader.Entities.Messages;
using BaseStationReader.Entities.Tracking;

namespace BaseStationReader.BusinessLogic.Simulator
{
    public class SurfacePositionMessageGenerator : MsgMessageGeneratorBase, IMessageGenerator
    {
        public SurfacePositionMessageGenerator(ITrackerLogger logger) : base(logger)
        {

        }

        /// <summary>
        /// Generate a Surface Position MSG message
        /// </summary>
        /// <param name="aircraft"></param>
        /// <returns></returns>
        public Message Generate(Aircraft aircraft)
        {
            // Generate the base message
            var message = ConstructMessage(TransmissionType.SurfacePosition, aircraft.Address);

            // Populate the type-specific members. Note that the messages don't attempt to simulate a realistic route
            // for an aircraft over time. They're just randomly selected values for properties
            message.Altitude = AltitudeToFeet(aircraft.Altitude.Value);
            message.GroundSpeed = GroundSpeedToKnots(aircraft.GroundSpeed.Value);
            message.Track = aircraft.Track;
            message.Latitude = aircraft.Latitude;
            message.Longitude = aircraft.Longitude;

            // Log and return the message
            LogGeneratedMessage(message);
            return message;
        }
    }
}

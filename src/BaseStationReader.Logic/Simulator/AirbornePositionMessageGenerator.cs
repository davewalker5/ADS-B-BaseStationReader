using BaseStationReader.Entities.Interfaces;
using BaseStationReader.Entities.Messages;

namespace BaseStationReader.Logic.Simulator
{
    public class AirbornePositionMessageGenerator : MessageGeneratorBase, IMessageGenerator
    {
        public AirbornePositionMessageGenerator(ITrackerLogger logger) : base(logger)
        {

        }

        /// <summary>
        /// Generate an Airborne Position MSG message
        /// </summary>
        /// <param name="address"></param>
        /// <param name="callsign"></param>
        /// <param name="squawk"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public Message Generate(string address, string? callsign, string? squawk)
        {
            // Generate the base message
            var message = ConstructMessage(TransmissionType.AirbornePosition, address);

            // Populate the type-specific members. Note that the messages don't attempt to simulate a realistic route
            // for an aircraft over time. They're just randomly selected values for properties
            message.Altitude = RandomInt(1000, 40000);
            message.Latitude = RandomInt(-90, 90);
            message.Longitude = RandomInt(-180, 190);

            // Log and return the message
            LogGeneratedMessage(message);
            return message;
        }
    }
}

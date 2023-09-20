using BaseStationReader.Entities.Interfaces;
using BaseStationReader.Entities.Messages;
using System.Diagnostics.CodeAnalysis;

namespace BaseStationReader.Logic.Simulator
{
    [ExcludeFromCodeCoverage]
    public class MsgMessageGenerator : IMessageGenerator
    {
        private readonly Random _random = new();

        /// <summary>
        /// Generate a random MSG message
        /// </summary>
        /// <param name="address"></param>
        /// <param name="callsign"></param>
        /// <returns></returns>
        public Message Generate(string address, string? callsign)
        {
            // Construct the basis of the message, including a random transmission type in the
            // range 1 to 8
            Message message = new()
            {
                MessageType = MessageType.MSG,
                TransmissionType = (TransmissionType)_random.Next(1, 9),
                Address = address,
                Callsign = callsign
            };

            // TODO: Populate the rest of the message based on the transmission type
            switch (message.TransmissionType)
            {
                case TransmissionType.Identification:
                    break;
                case TransmissionType.SurfacePosition:
                    break;
                case TransmissionType.AirbornePosition:
                    break;
                case TransmissionType.AirborneVelocity:
                    break;
                case TransmissionType.SurveillanceAlt:
                    break;
                case TransmissionType.SurveillanceId:
                    break;
                case TransmissionType.AirToAir:
                    break;
                case TransmissionType.AllCallReply:
                    break;
                default:
                    break;
            }

            return message;
        }
    }
}

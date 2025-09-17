using BaseStationReader.Entities.Interfaces;
using BaseStationReader.Entities.Messages;

namespace BaseStationReader.BusinessLogic.Messages
{
    public class MsgMessageParser : MessageParserBase, IMessageParser
    {
        /// <summary>
        /// Given a set of message fields for an MSG message, extract the data and return a Message
        /// instance
        /// /// </summary>
        /// <param name="fields"></param>
        /// <returns></returns>
        public Message Parse(string[] fields)
        {
            Message msg = ConstructMessage(MessageType.MSG, fields);

            var transmissionType = GetIntegerValue(fields, MessageField.TransmissionType);
            msg.TransmissionType = transmissionType != null ? (TransmissionType)transmissionType : TransmissionType.Unknown;
            msg.Callsign = GetStringValue(fields, MessageField.Callsign);
            msg.Altitude = GetDecimalValue(fields, MessageField.Altitude);
            msg.GroundSpeed = GetDecimalValue(fields, MessageField.GroundSpeed);
            msg.Track = GetDecimalValue(fields, MessageField.Track);
            msg.Latitude = GetDecimalValue(fields, MessageField.Latitude);
            msg.Longitude = GetDecimalValue(fields, MessageField.Longitude);
            msg.VerticalRate = GetDecimalValue(fields, MessageField.VerticalRate);
            msg.Squawk = GetStringValue(fields, MessageField.Squawk);
            msg.Alert = GetBooleanValue(fields, MessageField.Alert);
            msg.Emergency = GetBooleanValue(fields, MessageField.Emergency);
            msg.SPI = GetBooleanValue(fields, MessageField.SPI);
            msg.IsOnGround = GetBooleanValue(fields, MessageField.IsOnGround);

            return msg;
        }
    }
}
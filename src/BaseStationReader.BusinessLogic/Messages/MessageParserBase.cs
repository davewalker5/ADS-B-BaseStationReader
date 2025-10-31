using BaseStationReader.Entities.Messages;
using System.Globalization;

namespace BaseStationReader.BusinessLogic.Messages
{
    public abstract class MessageParserBase
    {
        private const string DATE_TIME_FORMAT = "yyyy/MM/dd HH:mm:ss.fff";

        /// <summary>
        /// Parse the common values out of a list of message fields to construct a basic message
        /// </summary>
        /// <param name="messageType"></param>
        /// <param name="fields"></param>
        /// <returns></returns>
        protected static Message ConstructMessage(MessageType messageType, string[] fields)
        {
            var address = GetStringValue(fields, MessageField.HexIdent) ?? "";
            var generated = GetTimestamp(fields, MessageField.DateGenerated, MessageField.TimeGenerated);

            // Note that we override the "last seen" timestamp (logged) in the incoming message as the application
            // has last seen the aircraft now, irrespective of what the message says
            var msg = new Message
            {
                MessageType = messageType,
                Address = address,
                Generated = generated,
                LastSeen = DateTime.Now
            };

            return msg;
        }

        /// <summary>
        /// Retrieve a string value from a field, returning null if the field is empty
        /// </summary>
        /// <param name="fields"></param>
        /// <param name="fieldIndex"></param>
        /// <returns></returns>
        protected static string GetStringValue(string[] fields, MessageField fieldIndex)
        {
            var valueString = fields[(int)fieldIndex].Trim();
            var value = (int)fieldIndex < fields.Length && valueString.Length > 0 ? valueString : null;
            return value;
        }

        /// <summary>
        /// Retrieve an integer value from a field, returning null if the field is empty
        /// </summary>
        /// <param name="fields"></param>
        /// <param name="fieldIndex"></param>
        protected static int? GetIntegerValue(string[] fields, MessageField fieldIndex)
        {
            int? value = null;

            var valueString = GetStringValue(fields, fieldIndex);
            if (valueString?.Length > 0 && int.TryParse(valueString, out int nonNullValue))
            {
                value = nonNullValue;
            }

            return value;
        }

        /// <summary>
        /// Retrieve a decimal value from a field, returning null if the field is empty
        /// </summary>
        /// <param name="fields"></param>
        /// <param name="fieldIndex"></param>
        protected static decimal? GetDecimalValue(string[] fields, MessageField fieldIndex)
        {
            decimal? value = null;

            var valueString = GetStringValue(fields, fieldIndex);
            if (valueString?.Length > 0 && decimal.TryParse(valueString, out decimal nonNullValue))
            {
                value = nonNullValue;
            }

            return value;
        }

        /// <summary>
        /// Retrieve a boolean value from a field, returning null if the field is empty
        /// </summary>
        /// <param name="fields"></param>
        /// <param name="fieldIndex"></param>
        protected static bool GetBooleanValue(string[] fields, MessageField fieldIndex)
        {
            bool value = false;

            var valueString = GetStringValue(fields, fieldIndex);
            if (valueString?.Length > 0)
            {
                _ = bool.TryParse(valueString, out value);
            }

            return value;
        }

        /// <summary>
        /// Retrieve date and time from two fields, one containing the date and the other the time
        /// </summary>
        /// </summary>
        /// <param name="fields"></param>
        /// <param name="dateFieldIdx"></param>
        /// <param name="timeFieldIdx"></param>
        /// <returns></returns>
        private static DateTime GetTimestamp(string[] fields, MessageField dateFieldIdx, MessageField timeFieldIdx)
        {
            var dateString = fields[(int)dateFieldIdx];
            var timeString = fields[(int)timeFieldIdx];
            var dateAndTime = $"{dateString} {timeString}";
            var timestamp = DateTime.ParseExact(dateAndTime, DATE_TIME_FORMAT, CultureInfo.InvariantCulture);
            return timestamp;
        }
    }
}
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace BaseStationReader.Entities.Messages
{
    [ExcludeFromCodeCoverage]
    public class Message
    {
        private const string DATE_FORMAT = "yyyy/MM/dd";
        private const string TIME_FORMAT = "HH:mm:ss.fff";

        public MessageType MessageType { get; set; }
        public TransmissionType TransmissionType { get; set; }
        public string Address { get; set; } = "";
        public DateTime Generated { get; set; }
        public DateTime LastSeen { get; set; }
        public string? Callsign { get; set; } = null;
        public decimal? Altitude { get; set; }
        public decimal? GroundSpeed { get; set; }
        public decimal? Track { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public decimal? VerticalRate { get; set; }
        public string? Squawk { get ; set; }
        public bool Alert { get; set; }
        public bool Emergency { get; set; }
        public bool SPI { get; set; }
        public bool IsOnGround { get; set; }

        /// <summary>
        /// Return a string representation of the message
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"{MessageType} {Address:X} {Generated.ToString()} {Callsign}";
        }

        /// <summary>
        /// Return the message in BaseStation format
        /// </summary>
        /// <returns></returns>
        public string ToBaseStation()
        {
            StringBuilder builder = new StringBuilder();
            AppendField(builder, MessageType);
            AppendField(builder, TransmissionType);
            AppendField(builder, null);
            AppendField(builder, null);
            AppendField(builder, Address);
            AppendField(builder, null);
            AppendField(builder, Generated.ToString(DATE_FORMAT));
            AppendField(builder, Generated.ToString(TIME_FORMAT));
            AppendField(builder, LastSeen.ToString(DATE_FORMAT));
            AppendField(builder, LastSeen.ToString(TIME_FORMAT));
            AppendField(builder, Callsign);
            AppendField(builder, Altitude);
            AppendField(builder, GroundSpeed);
            AppendField(builder, Track);
            AppendField(builder, Latitude);
            AppendField(builder, Longitude);
            AppendField(builder, VerticalRate);
            AppendField(builder, Squawk);
            AppendBooleanFiled(builder, Alert);
            AppendBooleanFiled(builder, Emergency);
            AppendBooleanFiled(builder, SPI);
            AppendBooleanFiled(builder, IsOnGround);
            return builder.ToString();
        }

        /// <summary>
        /// Append a field to a string builder representing a BaseStation format message
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="value"></param>
        private void AppendField(StringBuilder builder, object? value)
        {
            if (builder.Length > 0)
            {
                builder.Append(',');
            }

            if (value != null)
            {
                builder.Append(value.ToString());
            }
        }

        /// <summary>
        /// Append a boolean field to a string builder representing a BaseStation format message
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="value"></param>
        private void AppendBooleanFiled(StringBuilder builder, bool value)
        {
            var append = value ? "1" : "0";
            AppendField(builder, append);
        }
    }
}
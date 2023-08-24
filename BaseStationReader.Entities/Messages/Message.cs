using System.Diagnostics.CodeAnalysis;

namespace BaseStationReader.Entities.Messages
{
    [ExcludeFromCodeCoverage]
    public class Message
    {
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
        public bool IsOnGround { get; set; }

        public override string ToString()
        {
            return $"{MessageType} {Address:X} {Generated.ToString()} {Callsign}";
        }
    }
}
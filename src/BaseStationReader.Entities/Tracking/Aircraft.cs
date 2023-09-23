using BaseStationReader.Entities.Attributes;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace BaseStationReader.Entities.Tracking
{
    [ExcludeFromCodeCoverage]
    public class Aircraft : ICloneable
    {
        [Key]
        public int Id { get; set; }

        [Export("ICAO Address", 1)]
        [Required]
        public string Address { get; set; } = "";

        [Export("Callsign", 2)]
        public string? Callsign { get; set; } = null;

        [Export("Altitude", 4)]
        public decimal? Altitude { get; set; }

        [Export("Speed", 5)]
        public decimal? GroundSpeed { get; set; }

        [Export("Heading", 6)]
        public decimal? Track { get; set; }

        [Export("Latitude", 7)]
        public decimal? Latitude { get; set; }

        [Export("Longitude", 8)]
        public decimal? Longitude { get; set; }

        [Export("Vertical Rate", 9)]
        public decimal? VerticalRate { get; set; }

        [Export("Squawk", 3)]
        public string? Squawk { get; set; }

        [Export("First Seen", 10)]
        [Required]
        public DateTime FirstSeen { get; set; }

        [Export("Last Seen", 11)]
        [Required]
        public DateTime LastSeen { get; set; }

        [Required]
        public TrackingStatus Status { get; set; }

        [Export("Messages", 12)]
        public int Messages { get; set; }

        public object Clone()
        {
            return MemberwiseClone();
        }
    }
}
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

        [Export("Squawk", 3)]
        public string? Squawk { get; set; }

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

        [Export("Distance", 9)]
        public double? Distance { get; set;  }

        [Export("Vertical Rate", 10)]
        public decimal? VerticalRate { get; set; }

        [Export("First Seen", 11)]
        [Required]
        public DateTime FirstSeen { get; set; }

        [Export("Last Seen", 12)]
        [Required]
        public DateTime LastSeen { get; set; }

        [Export("Messages", 13)]
        public int Messages { get; set; }

        [Required]
        public TrackingStatus Status { get; set; }

#pragma warning disable CS8618
        public ICollection<AircraftPosition> Positions { get; set; }
#pragma warning restore CS8618

        public object Clone()
        {
            return MemberwiseClone();
        }
    }
}
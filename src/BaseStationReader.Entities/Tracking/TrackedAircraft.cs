using BaseStationReader.Entities.Attributes;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;

namespace BaseStationReader.Entities.Tracking
{
    [ExcludeFromCodeCoverage]
    public class TrackedAircraft : ICloneable
    {
        private const int MaximumHistoryEntries = 50;

        [Key]
        public int Id { get; set; }

        [Export("ICAO Address", 1)]
        [Required]
        public string Address { get; set; } = "";

        [Export("Callsign", 2)]
        public string Callsign { get; set; } = null;

        [Export("Squawk", 3)]
        public string Squawk { get; set; }

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
        public double? Distance { get; set; }

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

        [NotMapped]
        public AircraftBehaviour Behaviour { get; set; }

        [NotMapped]
        public int Lifespan { get; set; }

        [NotMapped]
        public DateTime PositionLastUpdated { get; set; }

        [NotMapped]
        public FixedSizeQueue<decimal> AltitudeHistory { get; private set; } = new(MaximumHistoryEntries);

        public object Clone()
        {
            return MemberwiseClone();
        }
    }
}
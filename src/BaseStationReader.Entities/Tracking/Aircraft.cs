using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace BaseStationReader.Entities.Tracking
{
    [ExcludeFromCodeCoverage]
    public class Aircraft : ICloneable
    {
        [Key]
        public int Id { get; set; }
        public string Address { get; set; } = "";
        public string? Callsign { get; set; } = null;
        public decimal? Altitude { get; set; }
        public decimal? GroundSpeed { get; set; }
        public decimal? Track { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public decimal? VerticalRate { get; set; }
        public string? Squawk { get; set; }
        public DateTime FirstSeen { get; set; }
        public DateTime LastSeen { get; set; }
        public bool Locked { get; set; }
        public Staleness Staleness { get; set; }

        public object Clone()
        {
            return MemberwiseClone();
        }
    }
}
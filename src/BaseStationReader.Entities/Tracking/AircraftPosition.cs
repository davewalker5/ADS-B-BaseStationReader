using BaseStationReader.Entities.Attributes;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;

namespace BaseStationReader.Entities.Tracking
{
    [ExcludeFromCodeCoverage]
    public class AircraftPosition
    {
        [Key]
        public int Id { get; set; }
        [ForeignKey("Aircraft.Id")]
        public int AircraftId { get; set; }

        [Export("Altitude", 2)]
        public decimal Altitude { get; set; }

        [Export("Latitude", 3)]
        public decimal Latitude { get; set; }

        [Export("Longitude", 4)]
        public decimal Longitude { get; set; }

        [Export("Timestamp", 5)]
        public DateTime Timestamp { get; set; }

        [Export("ICAO Address", 1)]
        public string Address { get; set; } = "";
    }
}

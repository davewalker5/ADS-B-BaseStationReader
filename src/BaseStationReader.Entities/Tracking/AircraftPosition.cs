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
        public decimal Altitude { get; set; }
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
        public DateTime Timestamp { get; set; }
        public string Address { get; set; } = "";
    }
}

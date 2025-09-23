using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;

namespace BaseStationReader.Entities.Lookup
{
    [ExcludeFromCodeCoverage]
    public class Sighting
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [ForeignKey(nameof(Lookup.Aircraft))]
        public int AircraftId { get; set; }

        [Required]
        [ForeignKey(nameof(Lookup.Flight))]
        public int FlightId { get; set; }

        [Required]
        public DateTime Timestamp { get; set; }

        public Aircraft Aircraft { get; set; }
        public Flight Flight { get; set; }
    }
}
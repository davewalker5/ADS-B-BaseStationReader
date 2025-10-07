using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;

namespace BaseStationReader.Entities.Api
{
    [ExcludeFromCodeCoverage]
    public class Sighting
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [ForeignKey(nameof(Api.Aircraft))]
        public int AircraftId { get; set; }

        [Required]
        [ForeignKey(nameof(Api.Flight))]
        public int FlightId { get; set; }

        [Required]
        public DateTime Timestamp { get; set; }

        public Aircraft Aircraft { get; set; }
        public Flight Flight { get; set; }
    }
}
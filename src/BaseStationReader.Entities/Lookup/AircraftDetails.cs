using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace BaseStationReader.Entities.Lookup
{
    [ExcludeFromCodeCoverage]
    public class AircraftDetails
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Address { get; set; } = "";

        public int? ModelId { get; set; }
        public int? AirlineId { get; set; }

        public Model? Model { get; set; }
        public Airline? Airline { get; set; }
    }
}

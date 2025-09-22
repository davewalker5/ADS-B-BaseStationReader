using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;

namespace BaseStationReader.Entities.Lookup
{
    [ExcludeFromCodeCoverage]
    public class Flight
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Number { get; set; } = "";

        [Required]
        public string ICAO { get; set; } = "";

        [Required]
        public string IATA { get; set; } = "";

        [Required]
        public string Embarkation { get; set; } = "";

        [Required]
        public string Destination { get; set; } = "";

        [Required]
        [ForeignKey(nameof(Airline))]
        public int AirlineId { get; set; }

        [NotMapped]
        public string ModelICAO { get; set; } = "";
        
        public Airline Airline { get; set; }
    }
}

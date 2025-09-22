using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;

namespace BaseStationReader.Entities.Lookup
{
    [ExcludeFromCodeCoverage]
    public class Airline
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string ICAO { get; set; } = "";

        [Required]
        public string IATA { get; set; } = "";

        [Required]
        public string Name { get; set; } = "";

        [NotMapped]
        public bool Active { get; set; } = true;
    }
}

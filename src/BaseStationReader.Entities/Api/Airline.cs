using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;

namespace BaseStationReader.Entities.Api
{
    [ExcludeFromCodeCoverage]
    public class Airline
    {
        [Key]
        public int Id { get; set; }

        public string ICAO { get; set; } = "";

        public string IATA { get; set; } = "";

        [Required]
        public string Name { get; set; } = "";

        public int ProvenanceId { get; set; }

        public Provenance Provenance { get; set; } = null!;

        [NotMapped]
        public string ProvenanceRef { get; set; } = "";

    }
}

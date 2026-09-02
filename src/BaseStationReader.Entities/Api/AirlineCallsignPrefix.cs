using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;

namespace BaseStationReader.Entities.Api
{
    [ExcludeFromCodeCoverage]
    public class AirlineCallsignPrefix
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(8)]
        public string Prefix { get; set; } = "";

        public int AirlineId { get; set; }

        public Airline Airline { get; set; } = null!;

        public int ProvenanceId { get; set; }

        public Provenance Provenance { get; set; } = null!;

        [NotMapped]
        public string AirlineIcaoRef { get; set; } = "";

        [NotMapped]
        public string ProvenanceRef { get; set; } = "";
    }
}

using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.ComponentModel.DataAnnotations.Schema;

namespace BaseStationReader.Entities.Api
{
    [ExcludeFromCodeCoverage]
    public class Manufacturer
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = "";

        public int ProvenanceId { get; set; }

        public Provenance Provenance { get; set; } = null!;

        [NotMapped]
        public string ProvenanceRef { get; set; } = "";
    }
}

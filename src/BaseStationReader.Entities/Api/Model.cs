using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;

#nullable enable

namespace BaseStationReader.Entities.Api
{
    [ExcludeFromCodeCoverage]
    public class Model
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = "";

        public string? ICAO { get; set; }

        public string? IATA { get; set; }

        [Required]
        [ForeignKey(nameof(Manufacturer))]
        public int ManufacturerId { get; set; }

        [NotMapped]
        public string ManufacturerName { get; set; } = "";

        public Manufacturer Manufacturer { get; set; } = null!;

        public int ProvenanceId { get; set; }

        public Provenance Provenance { get; set; } = null!;

        [NotMapped]
        public string ProvenanceRef { get; set; } = "";
    }
}

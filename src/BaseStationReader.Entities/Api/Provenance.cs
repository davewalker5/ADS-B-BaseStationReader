using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace BaseStationReader.Entities.Api
{
    [ExcludeFromCodeCoverage]
    public class Provenance
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string SourceRef { get; set; } = "";

        [Required]
        public string Source { get; set; } = "";

        [Required]
        public string SourceUrl { get; set; } = "";

        [Required]
        public string SourceDataset { get; set; } = "";

        [Required]
        public string SourceVersion { get; set; } = "";

        [Required]
        public string Licence { get; set; } = "";
    }
}

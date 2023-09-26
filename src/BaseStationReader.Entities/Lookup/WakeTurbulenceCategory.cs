using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace BaseStationReader.Entities.Lookup
{
    [ExcludeFromCodeCoverage]
    public class WakeTurbulenceCategory
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Category { get; set; } = "";

        [Required]
        public string Meaning { get; set; } = "";

#pragma warning disable CS8618
        public ICollection<AircraftModel> Models { get; set; }
#pragma warning restore CS8618
    }
}

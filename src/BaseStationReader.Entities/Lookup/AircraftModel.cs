using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace BaseStationReader.Entities.Lookup
{
    [ExcludeFromCodeCoverage]
    public class AircraftModel
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ManufacturerId { get; set; }

        public int? WakeTurbulenceCategoryId { get; set; }

        [Required]
        public string IATA { get; set; } = "";

        [Required]
        public string ICAO { get; set; } = "";

        [Required]
        public string Name { get; set; } = "";

#pragma warning disable CS8618
        public Manufacturer Manufacturer { get; set; }
#pragma warning restore CS8618
        public WakeTurbulenceCategory? WakeTurbulenceCategory { get; set; }
    }
}

using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace BaseStationReader.Entities.Lookup
{
    [ExcludeFromCodeCoverage]
    public class Manufacturer
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = "";

#pragma warning disable CS8618
        public ICollection<AircraftModel> Models { get; set; }
#pragma warning restore CS8618
    }
}

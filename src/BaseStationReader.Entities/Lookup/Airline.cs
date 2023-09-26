using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace BaseStationReader.Entities.Lookup
{
    [Serializable]
    [ExcludeFromCodeCoverage]
    public class Airline
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string IATA { get; set; } = "";

        [Required]
        public string ICAO { get; set; } = "";

        [Required]
        public string Name { get; set; } = "";
    }
}

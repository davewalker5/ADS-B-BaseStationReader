using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;

namespace BaseStationReader.Entities.Lookup
{
    [ExcludeFromCodeCoverage]
    public class Aircraft
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Address { get; set; } = "";

        [Required]
        public string Registration { get; set; } = "";

        public int? Manufactured { get; set; }

        public int? Age { get; set; }

        [Required]
        [ForeignKey(nameof(Model))]
        public int ModelId { get; set; }

        public Model Model { get; set; }
    }
}

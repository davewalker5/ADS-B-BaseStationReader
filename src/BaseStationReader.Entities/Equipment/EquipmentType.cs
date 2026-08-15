using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace BaseStationReader.Entities.Equipment
{
    /// <summary>
    /// Describes a category of receiver equipment.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public sealed class EquipmentType
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = "";

        public ICollection<Equipment> Equipment { get; set; } = [];
    }
}

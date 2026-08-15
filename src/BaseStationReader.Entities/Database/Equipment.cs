using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;

namespace BaseStationReader.Entities.Database
{
    /// <summary>
    /// Describes an item of equipment used by the receiver station.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public sealed class Equipment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = "";

        public int EquipmentTypeId { get; set; }

        [ForeignKey(nameof(EquipmentTypeId))]
        public EquipmentType EquipmentType { get; set; } = null!;
    }
}

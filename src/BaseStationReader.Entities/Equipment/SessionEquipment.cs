using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using BaseStationReader.Entities.Tracking;

namespace BaseStationReader.Entities.Equipment
{
    /// <summary>
    /// Associates an observation session with an item of equipment used during it.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public sealed class SessionEquipment
    {
        [Key]
        public int Id { get; set; }

        public int EquipmentId { get; set; }

        [ForeignKey(nameof(EquipmentId))]
        public Equipment Equipment { get; set; } = null!;

        public int SessionId { get; set; }

        [ForeignKey(nameof(SessionId))]
        public ObservationSession Session { get; set; } = null!;
    }
}

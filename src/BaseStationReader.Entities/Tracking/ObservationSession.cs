using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace BaseStationReader.Entities.Tracking
{
    [ExcludeFromCodeCoverage]
    public class ObservationSession
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public DateTime StartedAtUtc { get; set; }

        [Required]
        public string ProfileName { get; set; } = "";

        public string Notes { get; set; }

        [Required]
        public string Host { get; set; } = "";

        public int Port { get; set; }

        public double? ReceiverLatitude { get; set; }

        public double? ReceiverLongitude { get; set; }

        public int? ReceiverElevation { get; set; }

        public int? MinimumAltitude { get; set; }

        public int? MaximumAltitude { get; set; }

        public int? MaximumDistance { get; set; }

        [Required]
        public string IncludedBehaviours { get; set; } = "";

        public ICollection<TrackedAircraft> TrackedAircraft { get; set; } = [];
    }
}

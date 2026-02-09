using BaseStationReader.Entities.Attributes;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;

namespace BaseStationReader.Entities.Tracking
{
    [ExcludeFromCodeCoverage]
    public class AircraftPosition
    {
        [Key]
        public int Id { get; set; }

        [Export("ICAO Address", 1)]
        public string Address { get; set; } = "";

        [Export("Altitude", 2)]
        public decimal? Altitude { get; set; }

        [Export("Latitude", 3)]
        public decimal? Latitude { get; set; }

        [Export("Longitude", 4)]
        public decimal? Longitude { get; set; }

        [Export("Distance", 5)]
        public double? Distance { get; set; }

        [Required]
        [Export("Timestamp", 6)]
        public DateTime Timestamp { get; set; }

        [Required]
        [ForeignKey(nameof(TrackedAircraft))]
        public int AircraftId { get; set; }

        public TrackedAircraft Aircraft { get; set; }

        /// <summary>
        /// Create an aircraft position from a tracked aircraft
        /// </summary>
        /// <param name="aircraft"></param>
        /// <returns></returns>
        public static AircraftPosition FromTrackedAircraft(TrackedAircraft aircraft)
            => new ()
            {
                Address = aircraft.Address,
                Altitude = aircraft.Altitude,
                Distance = aircraft.Distance,
                Latitude = aircraft.Latitude,
                Longitude = aircraft.Longitude,
                Timestamp = aircraft.LastSeen
            };
    }
}

using BaseStationReader.Entities.Tracking;

namespace BaseStationReader.TrackerHub.Entities
{
    public class TrackedAircraftDto
    {
        public string Address { get; set; }
        public string Callsign { get; set; }
        public string Squawk { get; set; }
        public decimal? Altitude { get; set; }
        public AircraftBehaviour Behaviour { get; set; }
        public decimal? GroundSpeed { get; set; }
        public decimal? Track { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public double? Distance { get; set; }
        public decimal? VerticalRate { get; set; }
        public int Messages { get; set; }
        public DateTime LastSeen { get; set; }
        public TrackingStatus Status { get; set; }

        public static TrackedAircraftDto FromTrackedAircraft(TrackedAircraft a)
            => new()
            {
                Address = a.Address,
                Callsign = a.Callsign,
                Squawk = a.Squawk,
                Altitude = a.Altitude,
                Behaviour = a.Behaviour,
                GroundSpeed = a.GroundSpeed,
                Track = a.Track,
                Latitude = a.Latitude,
                Longitude = a.Longitude,
                Distance = a.Distance,
                VerticalRate = a.VerticalRate,
                Messages = a.Messages,
                LastSeen = a.LastSeen,
                Status = a.Status
            };
    }
}
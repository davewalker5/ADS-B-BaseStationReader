namespace BaseStationReader.Entities.Lookup
{
    public class LookupResult
    {
        public int? FlightId { get; set; }
        public int? AircraftId { get; set; }
        public int? SightingId { get; set; }
        public bool CreateSighting { get; set; }

        public bool IsSuccessful
        {
            get
            {
                return FlightId.HasValue && AircraftId.HasValue && (SightingId.HasValue || !CreateSighting);
            }
        }
    }
}
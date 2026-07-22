using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;

namespace BaseStationReader.Entities.Api
{
    [ExcludeFromCodeCoverage]
    public class Flight
    {
        private string _embarkation = "";
        private string _destination = "";

        [Key]
        public int Id { get; set; }

        public string ICAO { get; set; } = "";

        [Required]
        public string IATA { get; set; } = "";

        [Required]
        public string Callsign { get; set; } = "";

        [NotMapped]
        public string Embarkation
        {
            get => !string.IsNullOrWhiteSpace(_embarkation)
                ? _embarkation
                : OriginAirport?.IATA ?? OriginAirport?.ICAO ?? "";
            set => _embarkation = value ?? "";
        }

        [NotMapped]
        public string Destination
        {
            get => !string.IsNullOrWhiteSpace(_destination)
                ? _destination
                : DestinationAirport?.IATA ?? DestinationAirport?.ICAO ?? "";
            set => _destination = value ?? "";
        }

        [ForeignKey(nameof(Airline))]
        public int AirlineId { get; set; }

        public int OriginAirportId { get; set; }

        public int DestinationAirportId { get; set; }

        public int ProvenanceId { get; set; }

        [NotMapped]
        public string AircraftAddress { get; set; } = "";

        [NotMapped]
        public string ModelICAO { get; set; } = "";
        
        public Airline Airline { get; set; }

        public Airport OriginAirport { get; set; }

        public Airport DestinationAirport { get; set; }

        public Provenance Provenance { get; set; } = null!;

        [NotMapped]
        public string OriginICAO { get; set; } = "";

        [NotMapped]
        public string OriginIATA { get; set; } = "";

        [NotMapped]
        public string DestinationICAO { get; set; } = "";

        [NotMapped]
        public string DestinationIATA { get; set; } = "";

        [NotMapped]
        public string AirlineICAO { get; set; } = "";

        [NotMapped]
        public string AirlineIATA { get; set; } = "";

        [NotMapped]
        public string ProvenanceRef { get; set; } = "";
    }
}

using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace BaseStationReader.Entities.Lookup
{
    [ExcludeFromCodeCoverage]
    public class FlightDetails
    {
        [Required]
        public string Address { get; set; } = "";
        public string? DepartureAirportIATA { get; set; }
        public string? DepartureAirportICAO { get; set; }
        public string? DestinationAirportIATA { get; set; }
        public string? DestinationAirportICAO { get; set; }
        public string? FlightNumberIATA { get; set; }
        public string? FlightNumberICAO { get; set; }
    }
}

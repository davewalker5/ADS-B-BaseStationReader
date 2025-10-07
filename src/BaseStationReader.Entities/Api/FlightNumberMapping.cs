using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace BaseStationReader.Entities.Api
{
    [ExcludeFromCodeCoverage]
    public class FlightNumberMapping
    {
        [Key]
        public int Id { get; set; }
        public string AirlineICAO { get; set; }
        public string AirlineIATA { get; set; }
        public string FlightIATA { get; set; }
        public string Callsign { get; set; }
    }
}
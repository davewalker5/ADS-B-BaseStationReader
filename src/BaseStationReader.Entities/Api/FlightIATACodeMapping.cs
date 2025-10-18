using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace BaseStationReader.Entities.Api
{
    [ExcludeFromCodeCoverage]
    public class FlightIATACodeMapping
    {
        [Key]
        public int Id { get; set; }
        public string AirlineICAO { get; set; }
        public string AirlineIATA { get; set; }
        public string AirlineName { get; set; }
        public string AirportICAO { get; set; }
        public string AirportIATA { get; set; }
        public string AirportName { get; set; }
        public AirportType AirportType { get; set; }
        public string Embarkation { get; set; }
        public string Destination { get; set; }
        public string FlightIATA { get; set; }
        public string Callsign { get; set; }
        public string FileName { get; set; }
    }
}
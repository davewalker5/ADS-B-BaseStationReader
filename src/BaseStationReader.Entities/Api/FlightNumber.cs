using System.Diagnostics.CodeAnalysis;

namespace BaseStationReader.Entities.Api
{
    [ExcludeFromCodeCoverage]
    public class FlightNumber : FlightNumberMapping
    {
        public DateTime? Date { get; set; }

        public FlightNumber(string callsign, DateTime? date)
        {
            Callsign = callsign;
            Date = date;
        }

        public FlightNumber(FlightNumberMapping mapping, DateTime? date)
        {
            AirlineICAO = mapping.AirlineICAO;
            AirlineIATA = mapping.AirlineIATA;
            AirlineName = mapping.AirlineName;

            AirportICAO = mapping.AirportICAO;
            AirportIATA = mapping.AirportIATA;
            AirportName = mapping.AirportName;
            AirportType = mapping.AirportType;

            FlightIATA = mapping.FlightIATA;
            Embarkation = mapping.Embarkation;
            Destination = mapping.Destination;

            Callsign = mapping.Callsign;
            FileName = mapping.FileName;

            Date = date;
        }
    }
}
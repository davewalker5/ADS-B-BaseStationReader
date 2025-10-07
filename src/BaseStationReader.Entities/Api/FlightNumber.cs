using System.Diagnostics.CodeAnalysis;

namespace BaseStationReader.Entities.Api
{
    [ExcludeFromCodeCoverage]
    public class FlightNumber
    {
        public string Callsign { get; set; }
        public string Number { get; set; }
        public DateTime? Date { get; set; }

        public FlightNumber(string callsign, string number, DateTime? date)
        {
            Callsign = callsign;
            Number = number;
            Date = date;
        }
    }
}
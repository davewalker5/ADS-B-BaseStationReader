using System.Diagnostics.CodeAnalysis;
using BaseStationReader.Entities.Attributes;

namespace BaseStationReader.Entities.Api
{
    [ExcludeFromCodeCoverage]
    public class ExportableFlightNumber
    {
        private const string DateTimeFormat = "yyyy-MMM-dd HH:mm:ss";

        [Export("Callsign", 1)]
        public string Callsign { get; set; } = "";

        [Export("Number", 2)]
        public string Number { get; set; } = "";

        [Export("Date", 3)]
        public string Date { get; set; } = "";

        public static ExportableFlightNumber FromFlight(FlightNumber flight)
            => new()
            {
                Callsign = flight.Callsign,
                Number = flight.Number,
                Date = flight.Date != null ? flight.Date.Value.ToString(DateTimeFormat) : ""
            };
    }
}
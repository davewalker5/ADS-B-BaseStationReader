using System.Diagnostics.CodeAnalysis;
using BaseStationReader.Entities.Attributes;

namespace BaseStationReader.Entities.Api
{
    [ExcludeFromCodeCoverage]
    public class ExportableFlight
    {
        [Export("AircraftAddress", 1)]
        public string AircraftAddress { get; set; } = "";

        [Export("AirlineName", 2)]
        public string AirlineName { get; set; } = "";

        [Export("Number", 3)]
        public string Number { get; set; } = "";

        [Export("ICAO", 4)]
        public string ICAO { get; set; } = "";

        [Export("IATA", 5)]
        public string IATA { get; set; } = "";

        [Export("Embarkation", 6)]
        public string Embarkation { get; set; } = "";

        [Export("Destination", 7)]
        public string Destination { get; set; } = "";

        public static ExportableFlight FromFlight(Flight flight)
            => new()
            {
                Number = flight.Number,
                ICAO = flight.ICAO,
                IATA = flight.IATA,
                Embarkation = flight.Embarkation,
                Destination = flight.Destination,
                AirlineName = flight.Airline?.Name,
                AircraftAddress = flight.AircraftAddress
            };
    }
}
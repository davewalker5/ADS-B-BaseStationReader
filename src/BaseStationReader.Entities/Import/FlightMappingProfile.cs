using System.Diagnostics.CodeAnalysis;
using BaseStationReader.Entities.Api;
using CsvHelper.Configuration;

namespace BaseStationReader.Entities.Import
{
    [ExcludeFromCodeCoverage]
    public sealed class FlightMappingProfile : ClassMap<Flight>
    {
        public FlightMappingProfile()
        {
            Map(m => m.Callsign).Name("Callsign");
            Map(m => m.IATA).Name("Flight IATA");
            Map(m => m.ICAO).Name("Flight ICAO");
            Map(m => m.OriginICAO).Name("Origin ICAO");
            Map(m => m.OriginIATA).Name("Origin IATA");
            Map(m => m.DestinationICAO).Name("Destination ICAO");
            Map(m => m.DestinationIATA).Name("Destination IATA");
            Map(m => m.AirlineICAO).Name("Airline ICAO");
            Map(m => m.AirlineIATA).Name("Airline IATA");
            Map(m => m.ProvenanceRef).Name("Provenance");
        }
    }
}

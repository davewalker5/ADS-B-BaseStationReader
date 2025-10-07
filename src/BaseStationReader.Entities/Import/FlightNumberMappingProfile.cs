using System.Diagnostics.CodeAnalysis;
using BaseStationReader.Entities.Api;
using CsvHelper.Configuration;

namespace BaseStationReader.Entities.Import
{
    [ExcludeFromCodeCoverage]
    public sealed class FlightNumberMappingProfile : ClassMap<FlightNumberMapping>
    {
        public FlightNumberMappingProfile()
        {
            Map(m => m.AirlineICAO).Name("airline_icao");
            Map(m => m.AirlineIATA).Name("airline_iata");
            Map(m => m.Callsign).Name("callsign");
            Map(m => m.FlightIATA).Name("flight_iata");
        }
    }
}
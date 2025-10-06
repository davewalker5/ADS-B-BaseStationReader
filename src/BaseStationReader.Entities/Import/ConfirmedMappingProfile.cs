using System.Diagnostics.CodeAnalysis;
using BaseStationReader.Entities.Heuristics;
using CsvHelper.Configuration;

namespace BaseStationReader.Entities.Import
{
    [ExcludeFromCodeCoverage]
    public sealed class ConfirmedMappingProfile : ClassMap<ConfirmedMapping>
    {
        public ConfirmedMappingProfile()
        {
            Map(m => m.AirlineICAO).Name("airline_icao");
            Map(m => m.AirlineIATA).Name("airline_iata");
            Map(m => m.FlightIATA).Name("iata_flight");
            Map(m => m.Callsign).Name("callSign");
            Map(m => m.Digits).Name("digits");
        }
    }
}
using System.Diagnostics.CodeAnalysis;
using BaseStationReader.Entities.Api;
using CsvHelper.Configuration;

namespace BaseStationReader.Entities.Import
{
    [ExcludeFromCodeCoverage]
    public sealed class FlightIATACodeMappingProfile : ClassMap<FlightIATACodeMapping>
    {
        public FlightIATACodeMappingProfile()
        {
            Map(m => m.AirlineICAO).Name("airline_icao");
            Map(m => m.AirlineIATA).Name("airline_iata");
            Map(m => m.AirlineName).Name("airline_name");
            Map(m => m.AirportICAO).Name("airport_icao");
            Map(m => m.AirportIATA).Name("airport_iata");
            Map(m => m.AirportName).Name("airport_name");

            // Custom conversion of the direction column in the input data to a member
            // of the AirportType enumeration indicating flight direction
            Map(m => m.AirportType).Name("direction").TypeConverter<AirportTypeConverter>();

            Map(m => m.Callsign).Name("callsign");
            Map(m => m.FlightIATA).Name("flight_iata");
            Map(m => m.Embarkation).Name("embarkation");
            Map(m => m.Destination).Name("destination");
            Map(m => m.FileName).Name("filename");
        }
    }
}
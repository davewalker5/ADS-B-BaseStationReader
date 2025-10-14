using System.Diagnostics.CodeAnalysis;
using BaseStationReader.Entities.Api;
using CsvHelper.Configuration;

namespace BaseStationReader.Entities.Import
{
    [ExcludeFromCodeCoverage]
    public sealed class AircraftMappingProfile : ClassMap<Aircraft>
    {
        public AircraftMappingProfile()
        {
            Map(m => m.Address).Name("Address");
            Map(m => m.Registration).Name("Registration");
            Map(m => m.ModelIATA).Name("IATA");
            Map(m => m.ModelICAO).Name("ICAO");
            Map(m => m.Manufactured).TypeConverter<NullableIntegerTypeConverter>();
        }
    }
}
using System.Diagnostics.CodeAnalysis;
using BaseStationReader.Entities.Heuristics;
using CsvHelper.Configuration;

namespace BaseStationReader.Entities.Import
{
    [ExcludeFromCodeCoverage]
    public sealed class AirlineConstantsMappingProfile : ClassMap<AirlineConstants>
    {
        public AirlineConstantsMappingProfile()
        {
            Map(m => m.AirlineICAO).Name("airline_icao");
            Map(m => m.AirlineIATA).Name("airline_iata");
            Map(m => m.ConstantDelta).Name("constant_delta");
            Map(m => m.ConstantDeltaPurity).Name("constant_delta_purity");
            Map(m => m.ConstantPrefix).Name("constant_prefix");
            Map(m => m.IdentityRate).Name("identity_rate");
        }
    }
}
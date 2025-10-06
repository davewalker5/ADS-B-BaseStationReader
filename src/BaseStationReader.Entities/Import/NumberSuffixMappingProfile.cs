using System.Diagnostics.CodeAnalysis;
using BaseStationReader.Entities.Heuristics;
using CsvHelper.Configuration;

namespace BaseStationReader.Entities.Import
{
    [ExcludeFromCodeCoverage]
    public sealed class NumberSuffixRuleMappingProfile : ClassMap<NumberSuffixRule>
    {
        public NumberSuffixRuleMappingProfile()
        {
            Map(m => m.AirlineICAO).Name("airline_icao");
            Map(m => m.AirlineIATA).Name("airline_iata");
            Map(m => m.Numeric).Name("num");
            Map(m => m.Suffix).Name("suffix");
            Map(m => m.Digits).Name("digits");
            Map(m => m.Support).Name("support");
            Map(m => m.Purity).Name("purity");
        }
    }
}
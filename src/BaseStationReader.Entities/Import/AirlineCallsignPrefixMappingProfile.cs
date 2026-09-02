using BaseStationReader.Entities.Api;
using CsvHelper.Configuration;
using System.Diagnostics.CodeAnalysis;

namespace BaseStationReader.Entities.Import
{
    [ExcludeFromCodeCoverage]
    public sealed class AirlineCallsignPrefixMappingProfile : ClassMap<AirlineCallsignPrefix>
    {
        public AirlineCallsignPrefixMappingProfile()
        {
            Map(m => m.Prefix).Name("Prefix");
            Map(m => m.AirlineIcaoRef).Name("AirlineICAO");
            Map(m => m.ProvenanceRef).Name("Provenance");
        }
    }
}

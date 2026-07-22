using System.Diagnostics.CodeAnalysis;
using BaseStationReader.Entities.Api;
using CsvHelper.Configuration;

namespace BaseStationReader.Entities.Import
{
    [ExcludeFromCodeCoverage]
    public sealed class ProvenanceMappingProfile : ClassMap<Provenance>
    {
        public ProvenanceMappingProfile()
        {
            Map(m => m.SourceRef).Name("SourceRef");
            Map(m => m.Source).Name("Source");
            Map(m => m.SourceUrl).Name("SourceUrl");
            Map(m => m.SourceDataset).Name("SourceDataset");
            Map(m => m.SourceVersion).Name("SourceVersion");
            Map(m => m.Licence).Name("Licence");
        }
    }
}

using System.Diagnostics.CodeAnalysis;
using BaseStationReader.Entities.Api;
using CsvHelper.Configuration;

namespace BaseStationReader.Entities.Import
{
    [ExcludeFromCodeCoverage]
    public sealed class ManufacturerMappingProfile : ClassMap<Manufacturer>
    {
        public ManufacturerMappingProfile()
        {
            Map(m => m.Name).Name("Name");
        }
    }
}
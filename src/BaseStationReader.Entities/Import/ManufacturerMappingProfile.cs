using BaseStationReader.Entities.Lookup;
using CsvHelper.Configuration;

namespace BaseStationReader.Entities.Import
{
    public sealed class ManufacturerMappingProfile : ClassMap<Manufacturer>
    {
        public ManufacturerMappingProfile()
        {
            Map(m => m.Name).Name("Name");
        }
    }
}
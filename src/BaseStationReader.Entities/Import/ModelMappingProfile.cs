using BaseStationReader.Entities.Lookup;
using CsvHelper.Configuration;

namespace BaseStationReader.Entities.Import
{
    public sealed class ModelMappingProfile : ClassMap<Model>
    {
        public ModelMappingProfile()
        {
            Map(m => m.ICAO).Name("ICAO");
            Map(m => m.IATA).Name("IATA");
            Map(m => m.Name).Name("Name");
            Map(m => m.ManufacturerName).Name("Manufacturer");
        }
    }
}
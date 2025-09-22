using BaseStationReader.Entities.Import;
using BaseStationReader.Entities.Lookup;

namespace BaseStationReader.Entities.Interfaces
{
    public interface IManufacturerImporter : ICsvImporter<ManufacturerMappingProfile, Manufacturer>
    {
    }
}
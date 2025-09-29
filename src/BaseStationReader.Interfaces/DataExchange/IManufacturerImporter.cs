using BaseStationReader.Entities.Import;
using BaseStationReader.Entities.Lookup;

namespace BaseStationReader.Interfaces.DataExchange
{
    public interface IManufacturerImporter : ICsvImporter<ManufacturerMappingProfile, Manufacturer>
    {
    }
}
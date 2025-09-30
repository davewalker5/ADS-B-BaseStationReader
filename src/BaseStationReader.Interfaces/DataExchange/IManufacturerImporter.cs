using BaseStationReader.Entities.Import;
using BaseStationReader.Entities.Api;

namespace BaseStationReader.Interfaces.DataExchange
{
    public interface IManufacturerImporter : ICsvImporter<ManufacturerMappingProfile, Manufacturer>
    {
    }
}
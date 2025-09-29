using BaseStationReader.Entities.Import;
using BaseStationReader.Entities.Lookup;

namespace BaseStationReader.Interfaces.DataExchange
{
    public interface IModelImporter : ICsvImporter<ModelMappingProfile, Model>
    {
    }
}
using BaseStationReader.Entities.Import;
using BaseStationReader.Entities.Api;

namespace BaseStationReader.Interfaces.DataExchange
{
    public interface IModelImporter : ICsvImporter<ModelMappingProfile, Model>
    {
    }
}
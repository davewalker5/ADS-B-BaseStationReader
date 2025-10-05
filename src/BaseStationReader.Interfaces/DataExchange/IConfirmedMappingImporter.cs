using BaseStationReader.Entities.Import;
using BaseStationReader.Entities.Messages;

namespace BaseStationReader.Interfaces.DataExchange
{
    public interface IConfirmedMappingImporter : ICsvImporter<ConfirmedMappingProfile, ConfirmedMapping>
    {
    }
}
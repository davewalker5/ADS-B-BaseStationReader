using BaseStationReader.Entities.Api;

namespace BaseStationReader.Interfaces.Api
{
    public interface IHistoricalFlightApiWrapper : IFlightApiWrapper
    {
        bool SupportsLookupBy(ApiProperty propertyType);
    }
}
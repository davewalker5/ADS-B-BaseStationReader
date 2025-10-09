using BaseStationReader.Entities.Api;

namespace BaseStationReader.Interfaces.Api
{
    public interface IActiveFlightApiWrapper : IFlightApiWrapper
    {
        bool SupportsLookupBy(ApiProperty propertyType);
        Task<List<Flight>> LookupFlightsInBoundingBoxAsync(double centreLatitude, double centreLongitude, double rangeNm);
    }
}
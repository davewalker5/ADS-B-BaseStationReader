using BaseStationReader.Entities.Api;

namespace BaseStationReader.Interfaces.Api
{
    public interface IActiveFlightsApi : IExternalApi
    {
        bool SupportsLookupBy(ApiProperty propertyType);
        Task<Dictionary<ApiProperty, string>> LookupFlightAsync(ApiProperty propertyType, string propertyValue);
        Task<List<Dictionary<ApiProperty, string>>> LookupFlightsInBoundingBoxAsync(double centreLatitude, double centreLongitude, double rangeNm);
    }
}
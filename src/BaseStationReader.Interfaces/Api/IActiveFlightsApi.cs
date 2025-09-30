using BaseStationReader.Entities.Api;

namespace BaseStationReader.Interfaces.Api
{
    public interface IActiveFlightsApi : IExternalApi
    {
        Task<Dictionary<ApiProperty, string>> LookupFlightByAircraftAsync(string address);
        Task<List<Dictionary<ApiProperty, string>>> LookupFlightsInBoundingBox(double centreLatitude, double centreLongitude, double rangeNm);
    }
}
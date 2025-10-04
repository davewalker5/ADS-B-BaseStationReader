using BaseStationReader.Entities.Api;

namespace BaseStationReader.Interfaces.Api
{
    public interface IActiveFlightsApi : IExternalApi
    {
        Task<Dictionary<ApiProperty, string>> LookupFlight(string address);
        Task<List<Dictionary<ApiProperty, string>>> LookupFlightsInBoundingBox(double centreLatitude, double centreLongitude, double rangeNm);
    }
}
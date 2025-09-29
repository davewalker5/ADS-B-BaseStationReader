using BaseStationReader.Entities.Lookup;

namespace BaseStationReader.Interfaces.Api
{
    public interface IActiveFlightApi
    {
        Task<Dictionary<ApiProperty, string>> LookupFlightByAircraftAsync(string address);
        Task<List<Dictionary<ApiProperty, string>>> LookupFlightsInBoundingBox(
            double centreLatitude,
            double centreLongitude,
            double rangeNm);
    }
}
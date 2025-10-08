using BaseStationReader.Entities.Api;

namespace BaseStationReader.Interfaces.Api
{
    public interface IHistoricalFlightsApi : IExternalApi
    {
        bool SupportsLookupBy(ApiProperty propertyType);
        Task<List<Dictionary<ApiProperty, string>>> LookupFlightsByAircraftAsync(string address, DateTime date);
    }
}
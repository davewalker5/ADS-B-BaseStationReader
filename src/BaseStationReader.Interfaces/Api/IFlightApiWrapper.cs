using BaseStationReader.Entities.Api;
using BaseStationReader.Entities.Tracking;

namespace BaseStationReader.Interfaces.Api
{
    public interface IFlightApiWrapper
    {
        bool SupportsLookupBy(ApiProperty propertyType);
        Task<Flight> LookupFlightAsync(ApiLookupRequest request);
        Task<Flight> CreateFlightFromMapping(ApiLookupRequest request, FlightIATACodeMapping mapping);
    }
}
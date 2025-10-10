using BaseStationReader.Entities.Api;

namespace BaseStationReader.Interfaces.Api
{
    public interface IActiveFlightApiWrapper : IFlightApiWrapper
    {
        Task<List<Flight>> LookupFlightsInBoundingBoxAsync(double centreLatitude, double centreLongitude, double rangeNm);
    }
}
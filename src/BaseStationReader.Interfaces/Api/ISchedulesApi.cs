using System.Text.Json.Nodes;

namespace BaseStationReader.Interfaces.Api
{
    public interface ISchedulesApi : IExternalApi
    {
        Task<JsonNode> LookupSchedulesRawAsync(string iata, DateTime from, DateTime to);
    }
}
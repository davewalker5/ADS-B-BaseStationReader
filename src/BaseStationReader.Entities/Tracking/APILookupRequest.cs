using System.Diagnostics.CodeAnalysis;

namespace BaseStationReader.Entities.Tracking
{
    [ExcludeFromCodeCoverage]
    public class APILookupRequest
    {
        public string Address { get; set; } = "";
    }
}
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace BaseStationReader.Entities.Api
{
    [ExcludeFromCodeCoverage]
    public class ExcludedCallsign
    {
        [Key]
        public int Id { get; set; }
        public string Callsign { get; set; }
    }
}
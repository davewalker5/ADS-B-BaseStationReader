using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace BaseStationReader.Entities.Api
{
    [ExcludeFromCodeCoverage]
    public class ApiLogEntry
    {
        [Key]
        public int Id { get; set; }
        public string Service { get; set; }
        public string Endpoint { get; set; }
        public string Url { get; set; }
        public string Property { get; set; }
        public string PropertyValue { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
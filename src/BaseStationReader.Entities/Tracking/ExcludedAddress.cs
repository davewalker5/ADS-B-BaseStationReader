using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace BaseStationReader.Entities.Tracking
{
    [ExcludeFromCodeCoverage]
    public class ExcludedAddress
    {
        [Key]
        public int Id { get; set; }
        public string Address { get; set; }
    }
}
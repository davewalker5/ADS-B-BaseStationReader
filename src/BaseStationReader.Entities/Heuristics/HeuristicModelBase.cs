using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace BaseStationReader.Entities.Messages
{
    [ExcludeFromCodeCoverage]
    public class HeuristicModelBase
    {
        [Key]
        public int Id { get; set; }
        public string AirlineICAO { get; set; }
        public string AirlineIATA { get; set; }
    }
}
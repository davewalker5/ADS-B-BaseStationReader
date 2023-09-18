using System;

namespace BaseStationReader.UI.Models
{
    public class DatabaseSearchCriteria : BaseFilters
    {
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
    }
}

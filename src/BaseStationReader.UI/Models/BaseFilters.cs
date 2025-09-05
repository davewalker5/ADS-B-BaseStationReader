using BaseStationReader.UI.ViewModels;

namespace BaseStationReader.UI.Models
{
    public class BaseFilters : ViewModelBase
    {
        public string Address { get; set; } = "";
        public string Callsign { get; set; } = "";
        public string Status { get; set; } = "";
    }
}

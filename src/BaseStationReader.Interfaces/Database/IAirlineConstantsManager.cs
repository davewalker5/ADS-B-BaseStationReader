using BaseStationReader.Entities.Heuristics;

namespace BaseStationReader.Interfaces.Database
{
    public interface IAirlineConstantsManager
    {
        Task Truncate();
        Task<AirlineConstants> AddAsync(
            string airlineICAO,
            string airlineIATA,
            int? delta,
            decimal purity,
            string prefix,
            decimal identityRate);
    }
}
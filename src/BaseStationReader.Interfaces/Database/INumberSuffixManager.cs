using BaseStationReader.Entities.Messages;

namespace BaseStationReader.Interfaces.Database
{
    public interface INumberSuffixManager
    {
        Task<NumberSuffix> AddAsync(
            string airlineICAO,
            string airlineIATA,
            string numeric,
            string suffix,
            string digits,
            int support,
            decimal purity);
    }
}
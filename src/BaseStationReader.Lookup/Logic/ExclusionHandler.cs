using BaseStationReader.BusinessLogic.Configuration;
using BaseStationReader.BusinessLogic.Logging;
using BaseStationReader.Entities.Config;
using BaseStationReader.Interfaces.Database;
using BaseStationReader.Interfaces.Logging;

namespace BaseStationReader.Lookup.Logic
{
    internal class ExclusionHandler : CommandHandlerBase
    {
        public ExclusionHandler(
            LookupToolApplicationSettings settings,
            LookupToolCommandLineParser parser,
            ITrackerLogger logger,
            IDatabaseManagementFactory factory) : base (settings, parser, logger, factory)
        {

        }

        /// <summary>
        /// Handle the add exclusion command
        /// </summary>
        /// <returns></returns>
        public async Task HandleAddAsync()
        {
            var address = Parser.GetValues(CommandLineOptionType.AddExclusion)[0];
            await Factory.ExcludedAddressManager.AddAsync(address);
        }

        /// <summary>
        /// Handle the add exclusion command
        /// </summary>
        /// <returns></returns>
        public async Task HandleListAsync()
        {
            var exclusions = await Factory.ExcludedAddressManager.ListAsync(x => true);
            foreach (var exclusion in exclusions)
            {
                Console.WriteLine(exclusion.Address);
            }
        }
    }
}
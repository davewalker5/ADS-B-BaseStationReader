using BaseStationReader.BusinessLogic.Configuration;
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
        /// Handle adding an aircraft address exclusion
        /// </summary>
        /// <returns></returns>
        public async Task HandleAddAddressExclusionAsync()
        {
            var address = Parser.GetValues(CommandLineOptionType.AddExcludedAddress)[0];
            await Factory.ExcludedAddressManager.AddAsync(address);
        }

        /// <summary>
        /// Handle adding a callsign exclusion
        /// </summary>
        /// <returns></returns>
        public async Task HandleAddCallsignExclusionAsync()
        {
            var callsign = Parser.GetValues(CommandLineOptionType.AddExcludedCallsign)[0];
            await Factory.ExcludedAddressManager.AddAsync(callsign);
        }

        /// <summary>
        /// Handle the add exclusion command
        /// </summary>
        /// <returns></returns>
        public async Task HandleListAsync()
        {
            // List excluded aircraft addresses
            var excludedAddresses = await Factory.ExcludedAddressManager.ListAsync(x => true);
            Console.WriteLine($"{excludedAddresses.Count} aircraft 24-bit ICAO address exclusion(s):\n");
            foreach (var exclusion in excludedAddresses)
            {
                Console.WriteLine(exclusion.Address);
            }

            // List excluded callsigns
            var excludedCallsigns = await Factory.ExcludedCallsignManager.ListAsync(x => true);
            Console.WriteLine($"{excludedCallsigns.Count} callsign exclusion(s):\n");
            foreach (var exclusion in excludedCallsigns)
            {
                Console.WriteLine(exclusion.Callsign);
            }
        }
    }
}
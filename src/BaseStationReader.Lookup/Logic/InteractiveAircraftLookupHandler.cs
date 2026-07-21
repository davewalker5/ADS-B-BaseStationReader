using BaseStationReader.BusinessLogic.Configuration;
using BaseStationReader.Entities.Config;
using BaseStationReader.Interfaces.Api;
using BaseStationReader.Interfaces.Database;
using BaseStationReader.Interfaces.Logging;

namespace BaseStationReader.Lookup.Logic;

/// <summary>
/// Handles interactive address-based aircraft lookups.
/// </summary>
internal sealed class InteractiveAircraftLookupHandler : LookupHandlerBase
{
    public InteractiveAircraftLookupHandler(
        LookupToolApplicationSettings settings,
        LookupToolCommandLineParser parser,
        ITrackerLogger logger,
        IDatabaseManagementFactory factory,
        IExternalApiFactory apiFactory) : base(settings, parser, logger, factory, apiFactory)
    {
    }

    /// <summary>
    /// Resolves an address through the local-first external API wrapper and displays the result.
    /// </summary>
    public async Task HandleAsync()
    {
        var address = Parser.GetValues(CommandLineOptionType.Aircraft)[0].Trim().ToUpperInvariant();
        var wrapper = GetWrapperInstance(Settings.AircraftApi);
        var aircraft = await wrapper.LookupAircraftAsync(address);
        new AircraftTabulator().Write(address, aircraft);
    }
}

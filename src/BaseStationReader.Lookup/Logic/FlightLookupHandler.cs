using BaseStationReader.BusinessLogic.Configuration;
using BaseStationReader.Entities.Config;
using BaseStationReader.Interfaces.Api;
using BaseStationReader.Interfaces.Database;
using BaseStationReader.Interfaces.Logging;

namespace BaseStationReader.Lookup.Logic;

/// <summary>
/// Handles interactive callsign-based flight lookups.
/// </summary>
internal sealed class FlightLookupHandler : LookupHandlerBase
{
    public FlightLookupHandler(
        LookupToolApplicationSettings settings,
        LookupToolCommandLineParser parser,
        ITrackerLogger logger,
        IDatabaseManagementFactory factory,
        IExternalApiFactory apiFactory) : base(settings, parser, logger, factory, apiFactory)
    {
    }

    /// <summary>
    /// Resolves a callsign through the local-first external API wrapper and displays the result.
    /// </summary>
    public async Task HandleAsync()
    {
        var callsign = Parser.GetValues(CommandLineOptionType.Flight)[0].Trim().ToUpperInvariant();
        var wrapper = GetWrapperInstance(Settings.FlightApi);
        var flight = await wrapper.LookupFlightAsync(string.Empty, callsign);
        new FlightTabulator().Write(callsign, flight);
    }
}

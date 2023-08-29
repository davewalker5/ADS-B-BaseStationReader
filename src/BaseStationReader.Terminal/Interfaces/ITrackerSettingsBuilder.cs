using BaseStationReader.Entities.Config;

namespace BaseStationReader.Terminal.Interfaces
{
    internal interface ITrackerSettingsBuilder
    {
        ApplicationSettings? BuildSettings(IEnumerable<string> args, string configJsonPath);
    }
}
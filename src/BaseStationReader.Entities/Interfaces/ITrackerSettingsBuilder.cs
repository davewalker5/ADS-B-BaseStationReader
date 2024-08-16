using BaseStationReader.Entities.Config;

namespace BaseStationReader.Entities.Interfaces
{
    public interface ITrackerSettingsBuilder
    {
        TrackerApplicationSettings? BuildSettings(ICommandLineParser parser, string configJsonPath);
    }
}
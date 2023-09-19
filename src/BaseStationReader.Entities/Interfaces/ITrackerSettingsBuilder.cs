using BaseStationReader.Entities.Config;

namespace BaseStationReader.Entities.Interfaces
{
    public interface ITrackerSettingsBuilder
    {
        TrackerApplicationSettings? BuildSettings(IEnumerable<string> args, string configJsonPath);
    }
}
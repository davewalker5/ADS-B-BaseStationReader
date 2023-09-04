using BaseStationReader.Entities.Config;

namespace BaseStationReader.Entities.Interfaces
{
    public interface ITrackerSettingsBuilder
    {
        ApplicationSettings? BuildSettings(IEnumerable<string> args, string configJsonPath);
    }
}
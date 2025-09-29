using BaseStationReader.Entities.Config;

namespace BaseStationReader.Interfaces.Config
{
    public interface ITrackerSettingsBuilder
    {
        TrackerApplicationSettings BuildSettings(
            ICommandLineParser parser,
            ITrackingProfileReaderWriter reader,
            string configJsonPath);
    }
}
using BaseStationReader.Entities.Config;

namespace BaseStationReader.Entities.Interfaces
{
    public interface ITrackerSettingsBuilder
    {
        TrackerApplicationSettings BuildSettings(
            ICommandLineParser parser,
            ITrackingProfileReaderWriter reader,
            string configJsonPath);
    }
}
using BaseStationReader.Entities.Config;

namespace BaseStationReader.Interfaces.Config
{
    public interface ILookupToolSettingsBuilder
    {
        LookupToolApplicationSettings BuildSettings(ICommandLineParser parser, string configJsonPath);
    }
}
using BaseStationReader.Entities.Config;

namespace BaseStationReader.Entities.Interfaces
{
    public interface ILookupToolSettingsBuilder
    {
        LookupToolApplicationSettings BuildSettings(ICommandLineParser parser, string configJsonPath);
    }
}
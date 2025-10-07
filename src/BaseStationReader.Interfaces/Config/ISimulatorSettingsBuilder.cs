using BaseStationReader.Entities.Config;

namespace BaseStationReader.Interfaces.Config
{
    public interface ISimulatorSettingsBuilder
    {
        SimulatorApplicationSettings BuildSettings(ICommandLineParser parser, string configJsonPath);
    }
}
using BaseStationReader.Entities.Config;

namespace BaseStationReader.Entities.Interfaces
{
    public interface ISimulatorSettingsBuilder
    {
        SimulatorApplicationSettings BuildSettings(ICommandLineParser parser, string configJsonPath);
    }
}
using BaseStationReader.Entities.Config;
using BaseStationReader.Entities.Interfaces;

namespace BaseStationReader.Entities.Interfaces
{
    public interface ISimulatorSettingsBuilder
    {
        SimulatorApplicationSettings? BuildSettings(ICommandLineParser parser, string configJsonPath);
    }
}
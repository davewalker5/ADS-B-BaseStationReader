using BaseStationReader.Entities.Config;

namespace BaseStationReader.Interfaces.Config
{
    public interface IHelpGenerator
    {
        void Generate(IEnumerable<CommandLineOption> options);
    }
}

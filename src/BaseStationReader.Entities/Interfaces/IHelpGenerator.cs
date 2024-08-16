using BaseStationReader.Entities.Config;

namespace BaseStationReader.Entities.Interfaces
{
    public interface IHelpGenerator
    {
        void Generate(IEnumerable<CommandLineOption> options);
    }
}

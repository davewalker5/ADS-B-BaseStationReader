using BaseStationReader.Entities.Interfaces;

namespace BaseStationReader.BusinessLogic.Configuration
{
    public abstract class SettingsBuilderBase<T> where T : class
    {
        /// <summary>
        /// Construct the application settings from the configuration file and any command line arguments
        /// </summary>
        /// <param name="parser"></param>
        /// <param name="configPath"></param>
        /// <returns></returns>
        public virtual T LoadSettings(ICommandLineParser parser, string configPath)
        {
            // See if a development configuration file exists and, if so, use that in place of the
            // file provided. For example, if the supplied file name is "appsettings.json", the development
            // version is "appsettings.Development.json"
            var fileName = Path.GetFileNameWithoutExtension(configPath);
            var extension = Path.GetExtension(configPath);
            var developmentConfigPath = $"{fileName}.Development{extension}";
            var useConfigPath = File.Exists(developmentConfigPath) ? developmentConfigPath : configPath;

            // Read the config file to provide default settings
            var settings = new ConfigReader<T>().Read(useConfigPath);
            return settings;
        }
    }
}
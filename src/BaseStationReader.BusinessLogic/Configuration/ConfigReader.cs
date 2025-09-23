using BaseStationReader.Entities.Interfaces;
using Microsoft.Extensions.Configuration;

namespace BaseStationReader.BusinessLogic.Configuration
{
    public class ConfigReader<T> : IConfigReader<T> where T : class
    {
        /// <summary>
        /// Load and return the application settings from the named JSON-format application settings file
        /// </summary>
        /// <returns></returns>
        public virtual T Read(string jsonFileName)
        {
            // Make sure the JSON file path is absolute, using the application's base directory if
            // it doesn't have a path
            var jsonFilePath = Path.GetFullPath(jsonFileName, AppContext.BaseDirectory);

            // See if the development config file exists and use it preferentially if it does
            var developmentJsonFileName = GetDevelopmentConfigFileName(jsonFilePath);
            var useJsonFileName = File.Exists(developmentJsonFileName) ? developmentJsonFileName : jsonFilePath;

            // Set up the configuration reader
            var basePath = AppContext.BaseDirectory;
            IConfiguration configuration = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile(useJsonFileName)
                .Build();

            // Read the application settings section
            IConfigurationSection section = configuration.GetSection("ApplicationSettings");
            var settings = section.Get<T>();

            return settings;
        }

        /// <summary>
        /// Construct the development config file given a production config file name
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        private string GetDevelopmentConfigFileName(string jsonFileName)
        {
            // See if a development configuration file exists and, if so, use that in place of the
            // file provided. For example, if the supplied file name is "appsettings.json", the development
            // version is "appsettings.Development.json"
            var path = Path.GetDirectoryName(jsonFileName);
            var fileName = Path.GetFileNameWithoutExtension(jsonFileName);
            var extension = Path.GetExtension(jsonFileName);
            var developmentConfigName = $"{fileName}.Development{extension}";
            var developmentConfigPath = Path.Combine(path, developmentConfigName);
            return developmentConfigPath;
        }
    }
}

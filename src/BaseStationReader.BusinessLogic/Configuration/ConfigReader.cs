using BaseStationReader.Entities.Config;
using BaseStationReader.Interfaces.Config;
using Microsoft.Extensions.Configuration;

namespace BaseStationReader.BusinessLogic.Configuration
{
    public class ConfigReader<T> : IConfigReader<T> where T : class, new()
    {
        /// <summary>
        /// Load and return the application settings from the named JSON-format application settings file
        /// </summary>
        /// <returns></returns>
        public virtual T Read(string jsonFileName)
        {
            // See if the development config file exists and use it preferentially if it does
            var useJsonFileName = ConfigFileResolver.ResolveConfigFilePath(jsonFileName);

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
    }
}

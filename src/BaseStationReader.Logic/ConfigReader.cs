using BaseStationReader.Entities.Config;
using Microsoft.Extensions.Configuration;
using System.Diagnostics.CodeAnalysis;

namespace BaseStationReader.Logic
{
    [ExcludeFromCodeCoverage]
    public static class ConfigReader
    {
        /// <summary>
        /// Load and return the application settings from the named JSON-format application settings file
        /// </summary>
        /// <returns></returns>
        public static ApplicationSettings? Read(string jsonFileName)
        {
            IConfiguration configuration = new ConfigurationBuilder()
                .AddJsonFile(jsonFileName)
                .Build();

            IConfigurationSection section = configuration.GetSection("ApplicationSettings");
            var settings = section.Get<ApplicationSettings>();
            return settings;
        }
    }
}

using BaseStationReader.Entities.Config;
using BaseStationReader.Entities.Tracking;
using Microsoft.Extensions.Configuration;
using System.Reflection;

namespace BaseStationReader.Logic.Configuration
{
    public static class ConfigReader
    {
        /// <summary>
        /// Load and return the application settings from the named JSON-format application settings file
        /// </summary>
        /// <returns></returns>
        public static ApplicationSettings? Read(string jsonFileName)
        {
            // Set up the configuration reader
            IConfiguration configuration = new ConfigurationBuilder()
                .AddJsonFile(jsonFileName)
                .Build();

            // Read the application settings section
            IConfigurationSection section = configuration.GetSection("ApplicationSettings");
            var settings = section.Get<ApplicationSettings>();

            // Remove columns for which the property isn't set
            settings!.Columns.RemoveAll(x => string.IsNullOrEmpty(x.Property));

            // Add to the column definitions the property info objects associated with the associated property
            // of the Aircraft object
            var allProperties = typeof(Aircraft).GetProperties(BindingFlags.Instance | BindingFlags.Public);
            foreach (var column in settings!.Columns)
            {
                column.Info = Array.Find(allProperties, x => x.Name == column.Property);
            }

            return settings;
        }
    }
}

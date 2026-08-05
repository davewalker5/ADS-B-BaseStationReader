using BaseStationReader.Entities.Config;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.Diagnostics.CodeAnalysis;

namespace BaseStationReader.Data
{
    public class BaseStationReaderDbContextFactory : IDesignTimeDbContextFactory<BaseStationReaderDbContext>
    {
        private const string DefaultSettingsFile = "appsettings.json";

        /// <summary>
        /// Creates a context for the real database using the default settings file.
        /// </summary>
        /// <param name="args">Design-time arguments.</param>
        /// <returns>Configured database context.</returns>
        [ExcludeFromCodeCoverage]
        public BaseStationReaderDbContext CreateDbContext(string[] args)
            => CreateDbContext(DefaultSettingsFile);

        /// <summary>
        /// Creates a context using a specified application settings file.
        /// </summary>
        /// <param name="configFile">Application settings file containing the database connection string.</param>
        /// <returns>Configured database context.</returns>
        [ExcludeFromCodeCoverage]
        public BaseStationReaderDbContext CreateDbContext(string configFile)
        {
            // Get the path to the configuration file
            var configPath = ConfigFileResolver.ResolveConfigFilePath(configFile);

            // Construct a configuration object that contains the key/value pairs from the settings file
            // at the root of the main applicatoin
            IConfigurationRoot configuration = new ConfigurationBuilder()
                                                    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                                                    .AddJsonFile(configPath)
                                                    .Build();

            // Use the configuration object to read the connection string
            var optionsBuilder = new DbContextOptionsBuilder<BaseStationReaderDbContext>();
            optionsBuilder.UseSqlite(configuration.GetConnectionString("BaseStationReaderDB"));

            // Construct and return a database context
            return new BaseStationReaderDbContext(optionsBuilder.Options);
        }

        /// <summary>
        /// Create an in-memory context for unit testing
        /// </summary>
        /// <returns></returns>
        public static BaseStationReaderDbContext CreateInMemoryDbContext()
        {
            var optionsBuilder = new DbContextOptionsBuilder<BaseStationReaderDbContext>();
            optionsBuilder.UseInMemoryDatabase(Guid.NewGuid().ToString());
            return new BaseStationReaderDbContext(optionsBuilder.Options);
        }
    }
}

namespace BaseStationReader.Entities.Config
{
    public static class ConfigFileResolver
    {
        /// <summary>
        /// Given a config file path, see if there is a development version of the file and, if so, return
        /// that rather than the original config file
        /// </summary>
        /// <param name="configFilePath"></param>
        /// <returns></returns>
        public static string ResolveConfigFilePath(string configFilePath)
        {
            // Make sure the config file path is absolute, using the application's base directory if
            // it doesn't have a path
            var absoluteConfigFilePath = Path.GetFullPath(configFilePath, AppContext.BaseDirectory);

            // Construct the full path to a development version of the config file
            var path = Path.GetDirectoryName(absoluteConfigFilePath);
            var fileName = Path.GetFileNameWithoutExtension(absoluteConfigFilePath);
            var extension = Path.GetExtension(absoluteConfigFilePath);
            var developmentConfigName = $"{fileName}.Development{extension}";
            var developmentConfigPath = Path.Combine(path, developmentConfigName);

            // Preferentially use the development version of the config file, if it exists
            var useConfigFilePath = File.Exists(developmentConfigPath) ? developmentConfigPath : absoluteConfigFilePath;
            return useConfigFilePath;
        }
    }
}
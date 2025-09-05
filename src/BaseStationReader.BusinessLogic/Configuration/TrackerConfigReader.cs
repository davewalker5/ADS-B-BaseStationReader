using BaseStationReader.Entities.Config;
using BaseStationReader.Entities.Tracking;
using System.Reflection;

namespace BaseStationReader.BusinessLogic.Configuration
{
    public class TrackerConfigReader : ConfigReader<TrackerApplicationSettings>
    {
        /// <summary>
        /// Read the aircraft tracker application settings
        /// </summary>
        /// <param name="jsonFileName"></param>
        /// <returns></returns>
        public override TrackerApplicationSettings Read(string jsonFileName)
        {
            // Read the basic settings
            var settings = base.Read(jsonFileName);

            // Remove columns for which the property isn't set
            settings!.Columns.RemoveAll(x => string.IsNullOrEmpty(x.Property));

            // Add to the column definitions the property info objects associated with the associated property
            // of the Aircraft object
            var allProperties = typeof(Aircraft).GetProperties(BindingFlags.Instance | BindingFlags.Public);
            foreach (var column in settings!.Columns)
            {
                column.Info = Array.Find(allProperties, x => x.Name == column.Property);

                // Determine the type name for this property
                column.TypeName = column.Info!.PropertyType.Name;
                if (column.TypeName.Contains("Nullable"))
                {
#pragma warning disable CS8602
                    column.TypeName = Nullable.GetUnderlyingType(column.Info!.PropertyType).Name;
#pragma warning restore CS8602
                }
            }

            return settings;
        }
    }
}

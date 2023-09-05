using BaseStationReader.Entities.Interfaces;
using BaseStationReader.Entities.Tracking;
using System.Text;

namespace BaseStationReader.Logic.DataExchange
{
    public class AircraftCsvExporter : AircraftExporterBase, IAircraftCsvExporter
    {
        /// <summary>
        /// Export the specified collection of aircraft as a CSV file
        /// </summary>
        /// <param name="aircraft"></param>
        /// <param name="fileName"></param>
        /// <param name="separator"></param>
        public void Export(IEnumerable<Aircraft> aircraft, string fileName, char separator)
        {
            using (var writer = new StreamWriter(fileName, false, Encoding.UTF8))
            {
                // Construct and write the column headers
                var header = string.Join(",", Properties.Keys);
                writer.WriteLine(header);

                // Iterate over the aircraft and construct an output line for each one
                foreach (var a in aircraft)
                {
                    var builder = new StringBuilder();
                    bool first = true;

                    // Iterate over the properties, extracting the value for each one and appending it
                    // to the current line with a column separator, as needed
                    foreach (var property in Properties.Values)
                    {
                        var value = property.GetValue(a, null);
                        if (!first) builder.Append(separator);
                        builder.Append(value);
                        first = false;
                    }

                    // Write the current line
                    writer.WriteLine(builder.ToString());
                }
            }
        }
    }
}

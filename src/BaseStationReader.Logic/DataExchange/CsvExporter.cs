using BaseStationReader.Entities.Interfaces;
using System.Text;

namespace BaseStationReader.Logic.DataExchange
{
    public class CsvExporter<T> : ExporterBase<T>, ICsvExporter<T> where T: class
    {
        /// <summary>
        /// Export a collection of entities as a CSV file
        /// </summary>
        /// <param name="aircraft"></param>
        /// <param name="fileName"></param>
        /// <param name="separator"></param>
        public void Export(IEnumerable<T> entities, string fileName, char separator)
        {
            using (var writer = new StreamWriter(fileName, false, Encoding.UTF8))
            {
                // Construct and write the column headers
                var header = string.Join(",", Properties.Keys);
                writer.WriteLine(header);

                // Iterate over the entities and construct an output line for each one
                foreach (var e in entities)
                {
                    var builder = new StringBuilder();
                    bool first = true;

                    // Iterate over the properties, extracting the value for each one and appending it
                    // to the current line with a column separator, as needed
                    foreach (var property in Properties.Values)
                    {
                        var value = property.GetValue(e, null);
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

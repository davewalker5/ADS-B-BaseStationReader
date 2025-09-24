using System.Text;
using BaseStationReader.Entities.Events;
using BaseStationReader.Entities.Interfaces;

namespace BaseStationReader.BusinessLogic.Export
{
        public class CsvExporter<T> : ExporterBase<T>, ICsvExporter<T> where T : class
    {
        protected string DateExportFormat { get; private set; }

        public event EventHandler<ExportEventArgs<T>> RecordExport;

        public CsvExporter(string dateExportFormat) : base()
        {
            DateExportFormat = dateExportFormat;
        }

        /// <summary>
        /// Export a collection of entities as a CSV file
        /// </summary>
        /// <param name="aircraft"></param>
        /// <param name="fileName"></param>
        /// <param name="separator"></param>
        public virtual void Export(IEnumerable<T> entities, string fileName, char separator)
        {
            long count = 0;

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
                        // Get the value for this property
                        var value = property.GetValue(e, null);

                        // Add the separator and opening quote
                        if (!first) builder.Append(separator);
                        builder.Append('"');

                        // Don't try to append null values
                        if (value != null)
                        {
                            // Date values are formatted in a specific manner, so see if this property is a non-null
                            // date
                            if (property.PropertyType == typeof(DateTime))
                            {
                                // It is a date, so format it and append the formatted string
                                var valueAsFormattedfDate = ((DateTime)value).ToString(DateExportFormat);
                                builder.Append(valueAsFormattedfDate);
                            }
                            else
                            {
                                // Not a date so append it directly
                                builder.Append(value);
                            }
                        }

                        // Add the closing quote
                        builder.Append('"');
                        first = false;
                    }

                    // Write the current line
                    writer.WriteLine(builder.ToString());

                    // Notify subscribers
                    count++;
                    RecordExport?.Invoke(this, new ExportEventArgs<T>
                    {
                        RecordCount = count,
                        RecordSource = e
                    });
                }
            }
        }
    }
}
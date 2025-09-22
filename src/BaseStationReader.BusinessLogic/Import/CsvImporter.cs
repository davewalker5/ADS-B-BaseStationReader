using BaseStationReader.Entities.Interfaces;
using BaseStationReader.Entities.Logging;
using CsvHelper;
using CsvHelper.Configuration;
using Serilog;
using System.Globalization;

namespace BaseStationReader.BusinessLogic.Logging
{
    public abstract class CsvImporter<M, T> : ICsvImporter<M,T>
        where M : ClassMap
        where T : class
    {
        public ITrackerLogger Logger { get; private set; }

        public CsvImporter(ITrackerLogger logger)
            => Logger = logger;

        /// <summary>
        /// Generate a collection of instances of type T from the contents of a CSV file
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public virtual List<T> Read(string filePath)
        {
            List<T> records = null;

            if (File.Exists(filePath))
            {
                Logger.LogMessage(Severity.Info, $"Loading CSV file '{filePath}'");

                using (var reader = new StreamReader(filePath))
                {
                    using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                    {
                        // Configure the mapping from column names to fields
                        csv.Context.RegisterClassMap<M>();

                        // Load the CSV file
                        records = [.. csv.GetRecords<T>()];
                    }
                }

                Logger.LogMessage(Severity.Info, $"{records.Count} records read");
            }
            else
            {
                Logger.LogMessage(Severity.Error, $"File '{filePath}' not found");
            }

            return records;
        }
    }
}
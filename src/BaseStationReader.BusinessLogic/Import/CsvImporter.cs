using BaseStationReader.Entities.Interfaces;
using BaseStationReader.Entities.Logging;
using CsvHelper;
using CsvHelper.Configuration;
using Serilog;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace BaseStationReader.BusinessLogic.Logging
{
    public abstract class CsvImporter<M, T> : ICsvImporter<M, T>
        where M : ClassMap
        where T : class
    {
        public HashSet<string> Replacements { get; private set; } = new(["-", "(undefined)", "n/a"], StringComparer.OrdinalIgnoreCase);
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

        /// <summary>
        /// Save a collection of entities to the database
        /// </summary>
        /// <param name="manufacturers"></param>
        /// <returns></returns>
#pragma warning disable CS1998
        [ExcludeFromCodeCoverage]
        public virtual async Task Save(IEnumerable<T> entities)
        {
            // This would be better as an abstract method but that's not possible with async
            // methods. The expectation is classes inheriting from this one *must* override
            // this method
            throw new NotImplementedException();
        }
#pragma warning restore CS1998

        /// <summary>
        /// Import a set of airline definitions into the database from a CSV file
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public async Task Import(string filePath)
        {
            var entities = Read(filePath);
            await Save(entities);
        }
    }
}
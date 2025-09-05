using BaseStationReader.Data;
using BaseStationReader.Entities.Expressions;
using BaseStationReader.Entities.Tracking;
using BaseStationReader.Logic.Database;
using BaseStationReader.Logic.DataExchange;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;

namespace BaseStationReader.UI.Models
{
    public class DatabaseSearchModel
    {
        public ObservableCollection<Aircraft> SearchResults { get; private set; } = new();
        public DatabaseSearchCriteria SearchCriteria { get; set; }

        /// <summary>
        /// Search the database for records matching the current filtering criteria
        /// </summary>
        public void Search()
        {
            // Create an expression builder and add an expression for each non-null/blak filtering criterion
            ExpressionBuilder<Aircraft> builder = new ExpressionBuilder<Aircraft>();

            if (!string.IsNullOrEmpty(SearchCriteria?.Address))
            {
                builder.Add("Address", TrackerFilterOperator.Equals, SearchCriteria.Address);
            }

            if (!string.IsNullOrEmpty(SearchCriteria?.Callsign))
            {
                builder.Add("Callsign", TrackerFilterOperator.Equals, SearchCriteria.Callsign);
            }

            if (!string.IsNullOrEmpty(SearchCriteria?.Status) && Enum.TryParse(SearchCriteria.Status, out TrackingStatus statusEnumValue))
            {
                builder.Add("Status", TrackerFilterOperator.Equals, statusEnumValue);
            }

            if (SearchCriteria?.From != null)
            {
                builder.Add("LastSeen", TrackerFilterOperator.GreaterThanOrEqual, SearchCriteria.From);
            }

            if (SearchCriteria?.To != null)
            {
                builder.Add("LastSeen", TrackerFilterOperator.LessThanOrEqual, SearchCriteria?.To);
            }

            // Create a database context and an instance of the (reader) writer
            var context = new BaseStationReaderDbContextFactory().CreateDbContext(Array.Empty<string>());
            var writer = new AircraftWriter(context);

            // Build the filter expression. If there is one, use it to filter the collection of aircraft used
            // to refresh the grid. Otherwise, just use all the current tracked aircraft
            var filter = builder.Build();
            List<Aircraft> aircraft;
            if (filter != null)
            {
                aircraft = Task.Run(() => writer.ListAsync(filter)).Result;
            }
            else
            {
                aircraft = Task.Run(() => writer.ListAsync(x => true)).Result;
            }

            // Update the observable collection from the filtered aircraft list
            SearchResults = new ObservableCollection<Aircraft>(aircraft);
        }

        /// <summary>
        /// Export the current search results to the specified file
        /// </summary>
        /// <param name="filePath"></param>
        public void Export(string filePath)
        {
            // Check we have a valid file path
            if (!string.IsNullOrEmpty(filePath))
            {
                // Use the extension to decide which exporter to use
                var extension = Path.GetExtension(filePath);
                switch (extension)
                {
                    case ".xlsx":
                        var xlsxExporter = new XlsxExporter<Aircraft>();
                        xlsxExporter.Export(SearchResults, filePath, "Aircraft");
                        break;
                    case ".csv":
                        var csvExporter = new CsvExporter<Aircraft>();
                        csvExporter.Export(SearchResults, filePath, ',');
                        break;
                    default:
                        break;
                }
            }
        }
    }
}

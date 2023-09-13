using BaseStationReader.Data;
using BaseStationReader.Entities.Expressions;
using BaseStationReader.Entities.Tracking;
using BaseStationReader.Logic.Database;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace BaseStationReader.UI.ViewModels
{
    internal class DatabaseSearchViewModel
    {
        public ObservableCollection<Aircraft> SearchResults { get; private set; } = new();

        /// <summary>
        /// Search the database for records matching the specified filtering criteria
        /// </summary>
        /// <param name="address"></param>
        /// <param name="callsign"></param>
        /// <param name="status"></param>
        /// <param name="from"></param>
        /// <param name="to"></param>
        public void Search(string? address, string? callsign, string? status, DateTime? from, DateTime? to)
        {
            // Create an expression builder and add an expression for each non-null/blak filtering criterion
            ExpressionBuilder<Aircraft> builder = new ExpressionBuilder<Aircraft>();

            if (!string.IsNullOrEmpty(address))
            {
                builder.Add("Address", TrackerFilterOperator.Equals, address);
            }

            if (!string.IsNullOrEmpty(callsign))
            {
                builder.Add("Callsign", TrackerFilterOperator.Equals, callsign);
            }

            if (!string.IsNullOrEmpty(status) && Enum.TryParse<TrackingStatus>(status, out TrackingStatus statusEnumValue))
            {
                builder.Add("Status", TrackerFilterOperator.Equals, statusEnumValue);
            }

            if (from != null)
            {
                builder.Add("LastSeen", TrackerFilterOperator.GreaterThanOrEqual, from);
            }

            if (to != null)
            {
                builder.Add("LastSeen", TrackerFilterOperator.LessThanOrEqual, to);
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
    }
}

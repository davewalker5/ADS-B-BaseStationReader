using BaseStationReader.Entities.Config;
using BaseStationReader.Interfaces.Tracking;
using BaseStationReader.Entities.Tracking;
using BaseStationReader.Terminal.Interfaces;
using Spectre.Console;

namespace BaseStationReader.Terminal.Logic
{
    internal class TrackerTableManager : ITrackerTableManager
    {
        private const string UpArrow = "\u2191";
        private const string DownArrow = "\u2193";
        private readonly object _lock = new();
        private readonly ITrackerIndexManager _indexManager;
        private readonly List<TrackerColumn> _columns;
        private readonly int _maximumRows;

        public Spectre.Console.Table Table { get; private set; } = null;

        public TrackerTableManager(ITrackerIndexManager indexManager, List<TrackerColumn> columns, int maximumRows)
        {
            _indexManager = indexManager;
            _columns = columns;
            _maximumRows = maximumRows;
        }

        /// <summary>
        /// Create the table
        /// </summary>
        /// <param name="title"></param>
        /// <returns></returns>
        public void CreateTable(string title)
        {
            var table = new Spectre.Console.Table().Expand().BorderColor(Spectre.Console.Color.Grey);

            // Set the table title
            table.Title(title);

            // Add the column titles
            foreach (var label in _columns.Select(x => x.Label))
            {
                table.AddColumn($"[yellow]{label}[/]");
            }

            // Expose the table via the public property
            Table = table;
        }

        /// <summary>
        /// Add an aircraft to the table and return its row number, which will either be 0 if successful or
        /// -1 if the maximum row limit has been exceeded
        /// </summary>
        /// <param name="aircraft"></param>
        /// <returns></returns>
        public int AddAircraft(TrackedAircraft aircraft)
        {
            var rowNumber = -1;

            lock (_lock)
            {
                rowNumber = AddTableRow(aircraft);
            }

            return rowNumber;
        }

        /// <summary>
        /// Update an aircraft's entry in the table
        /// </summary>
        /// <param name="aircraft"></param>
        public int UpdateAircraft(TrackedAircraft aircraft)
        {
            var rowNumber = -1;

            lock (_lock)
            {
                // Find the row number for the aircraft's ICAO address
                rowNumber = _indexManager.FindAircraft(aircraft.Address);
                if ((rowNumber >= 0) && (rowNumber < Table.Rows.Count))
                {
                    // Found, so a new row with updated row data, remove the old row and insert the new one
                    var rowData = GetAircraftRowData(aircraft);
                    Table.RemoveRow(rowNumber);
                    Table.InsertRow(rowNumber, rowData);
                }
                else
                {
                    // It's not there, which may mean an attempt was made to add it while the table was at
                    // capacity. Add it now
                    AddTableRow(aircraft);
                }
            }

            return rowNumber;
        }

        /// <summary>
        /// Remove an aircraft from the table and return the row number it occupied
        /// </summary>
        /// <param name="aircraft"></param>
        /// <returns></returns>
        public int RemoveAircraft(TrackedAircraft aircraft)
        {
            lock (_lock)
            {
                // Find the row number for the aircraft's ICAO address
                var rowNumber = _indexManager.RemoveAircraft(aircraft.Address);
                if ((rowNumber >= 0) && (rowNumber < Table.Rows.Count))
                {
                    // Found, so remove that row from the table
                    Table.RemoveRow(rowNumber);
                }

                return rowNumber;
            }
        }

        /// <summary>
        /// Add a row to the table and return its row number, which will either be 0 if successful or
        /// -1 if the maximum row limit has been exceeded
        /// </summary>
        /// <param name="aircraft"></param>
        /// <returns></returns>
        public int AddTableRow(TrackedAircraft aircraft)
        {
            var rowNumber = -1;

            // Check we've not exceeded the row limit
            if ((_maximumRows == 0) || (Table.Rows.Count < _maximumRows))
            {
                // Add the aircraft to the table
                rowNumber = 0;
                var rowData = GetAircraftRowData(aircraft);
                Table.InsertRow(0, rowData);

                // Update the address/row index
                _indexManager.AddAircraft(aircraft.Address, rowNumber);
            }

            return rowNumber;
        }

        /// <summary>
        /// Construct and return a row of data for the specified aircraft
        /// </summary>
        /// <param name="aircraft"></param>
        /// <returns></returns>
        private string[] GetAircraftRowData(TrackedAircraft aircraft)
        {
            // Use the aircraft's staleness to set the row colour
            var startColour = "";
            var endColour = "";
            if (aircraft.Status == TrackingStatus.Stale)
            {
                startColour = "[red]";
                endColour = "[/]";
            }
            else if (aircraft.Status == TrackingStatus.Inactive)
            {
                startColour = "[yellow]";
                endColour = "[/]";
            }

            // Determine the directional indicator
            var behaviour = aircraft.Behaviour switch
            {
                AircraftBehaviour.Descending => DownArrow,
                AircraftBehaviour.Climbing => UpArrow,
                AircraftBehaviour.LevelFlight => "=",
                _ => "?",
            };

            // Construct the row data
            var data = new List<string>();
            foreach (var column in _columns)
            {
                // Get the value for this column as a string
                var valueString = "";
                if (column.TypeName.Equals("Decimal", StringComparison.OrdinalIgnoreCase))
                {
                    decimal? value = (decimal?)column.Info.GetValue(aircraft);
                    valueString = value?.ToString(column.Format) ?? "";
                }
                else if (column.TypeName.Equals("double", StringComparison.OrdinalIgnoreCase))
                {
                    double? value = (double?)column.Info.GetValue(aircraft);
                    valueString = value?.ToString(column.Format) ?? "";
                }
                else if (column.TypeName.Equals("bool", StringComparison.OrdinalIgnoreCase))
                {
                    bool value = (bool?)column.Info.GetValue(aircraft) ?? false;
                    valueString = value ? "Yes" : "No";
                }
                else if (column.TypeName.Equals("DateTime", StringComparison.OrdinalIgnoreCase))
                {
                    DateTime? value = (DateTime?)column.Info.GetValue(aircraft);
                    valueString = value?.ToString(column.Format) ?? "";
                }
                else
                {
                    valueString = column.Info.GetValue(aircraft)?.ToString() ?? "";
                }

                // Determine the ascent/descent behaviour indicator
                var indicator = column.ShowBehaviour && !string.IsNullOrEmpty(valueString) ? $" {behaviour}" : "";

                // Construct and add the row
                data.Add($"{startColour}{valueString}{indicator}{endColour}");
            }

            return data.ToArray();
        }
    }
}

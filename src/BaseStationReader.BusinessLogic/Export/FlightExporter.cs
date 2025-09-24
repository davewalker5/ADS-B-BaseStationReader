using BaseStationReader.Entities.Events;
using BaseStationReader.Entities.Interfaces;
using BaseStationReader.Entities.Lookup;

namespace BaseStationReader.BusinessLogic.Export
{
    public class FlightExporter : IFlightExporter
    {
        public event EventHandler<ExportEventArgs<ExportableFlight>> RecordExport;

        public void Export(IEnumerable<Flight> flights, string file)
        {
            // Convert the flights to exportable flights (flattened hierarchy)
            var exportable = flights.Select(x => ExportableFlight.FromFlight(x));

            // Configure an exporter to export them
            var exporter = new CsvExporter<ExportableFlight>(null);
            exporter.RecordExport += OnRecordExported;

            // Export the records
            exporter.Export(exportable, file, ',');
        }

        /// <summary>
        /// Handler for flight export notifications
        /// </summary>
        /// <param name="_"></param>
        /// <param name="e"></param>
        private void OnRecordExported(object _, ExportEventArgs<ExportableFlight> e)
            => RecordExport?.Invoke(this, e);
    }
}
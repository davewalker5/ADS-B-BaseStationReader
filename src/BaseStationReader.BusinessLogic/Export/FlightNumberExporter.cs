using BaseStationReader.Entities.Events;
using BaseStationReader.Interfaces.DataExchange;
using BaseStationReader.Entities.Api;

namespace BaseStationReader.BusinessLogic.Export
{
    public class FlightNumberExporter : IFlightNumberExporter
    {
        public event EventHandler<ExportEventArgs<ExportableFlightNumber>> RecordExport;

        public void Export(IEnumerable<FlightNumber> flightNumbers, string file)
        {
            // Convert the flights to exportable flights (flattened hierarchy)
            var exportable = flightNumbers.Select(x => ExportableFlightNumber.FromFlight(x));

            // Configure an exporter to export them
            var exporter = new CsvExporter<ExportableFlightNumber>(null);
            exporter.RecordExport += OnRecordExported;

            // Export the records
            exporter.Export(exportable, file, ',');
        }

        /// <summary>
        /// Handler for flight number export notifications
        /// </summary>
        /// <param name="_"></param>
        /// <param name="e"></param>
        private void OnRecordExported(object _, ExportEventArgs<ExportableFlightNumber> e)
            => RecordExport?.Invoke(this, e);
    }
}
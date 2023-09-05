using BaseStationReader.Entities.Interfaces;
using BaseStationReader.Entities.Tracking;
using ClosedXML.Excel;

namespace BaseStationReader.Logic.DataExchange
{
    public class AircraftXlsxExporter : AircraftExporterBase, IAircraftXlsxExporter
    {
        /// <summary>
        /// Export a collection of aircraft to an Excel workbook
        /// </summary>
        /// <param name="aircraft"></param>
        /// <param name="fileName"></param>
        public void Export(IEnumerable<Aircraft> aircraft, string fileName)
        {
            // Open a workbook
            using (var workbook = new XLWorkbook())
            {
                // Add a worksheet to contain the aircraft
                var worksheet = workbook.Worksheets.Add("Aircraft");

                // Add the column titles
                var columnNumber = 1;
                foreach (var columnName in Properties.Keys)
                {
                    worksheet.Cell(1, columnNumber).Value = columnName;
                    columnNumber++;
                }

                // Iterate over the aircraft and construct an output line for each one
                var rowNumber = 2;
                foreach (var a in aircraft)
                {
                    // Iterate over the properties, extracting the value for each one and writing it
                    // to the current cell
                    columnNumber = 1;
                    foreach (var property in Properties.Values)
                    {
                        var value = property.GetValue(a, null);
                        if (value != null)
                        {
                            worksheet.Cell(rowNumber, columnNumber).Value = value!.ToString();
                        }
                        columnNumber++;
                    }

                    // Move on to the next row
                    rowNumber++;
                }

                // Save the workbook to the specified file
                workbook.SaveAs(fileName);
            }
        }
    }
}

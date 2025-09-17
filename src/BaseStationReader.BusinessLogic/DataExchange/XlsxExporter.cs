using BaseStationReader.Entities.Interfaces;
using ClosedXML.Excel;

namespace BaseStationReader.BusinessLogic.DataExchange
{
    public class XlsxExporter<T> : ExporterBase<T>, IXlsxExporter<T> where T: class
    {
        /// <summary>
        /// Export a collection of entities to an Excel workbook
        /// </summary>
        /// <param name="entities"></param>
        /// <param name="fileName"></param>
        /// <param name="worksheetName"></param>
        public void Export(IEnumerable<T> entities, string fileName, string worksheetName)
        {
            // Open a workbook
            using (var workbook = new XLWorkbook())
            {
                // Add a worksheet to contain the entities
                var worksheet = workbook.Worksheets.Add(worksheetName);

                // Add the column titles
                var columnNumber = 1;
                foreach (var columnName in Properties.Keys)
                {
                    worksheet.Cell(1, columnNumber).Value = columnName;
                    columnNumber++;
                }

                // Iterate over the entities
                var rowNumber = 2;
                foreach (var e in entities)
                {
                    // Iterate over the properties, extracting the value for each one and writing it
                    // to the current cell
                    columnNumber = 1;
                    foreach (var property in Properties.Values)
                    {
                        var value = property.GetValue(e, null);
                        if (value != null)
                        {
                            worksheet.Cell(rowNumber, columnNumber).Value = value.ToString();
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

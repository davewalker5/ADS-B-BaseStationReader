using BaseStationReader.Entities.Api;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;

namespace BaseStationReader.Entities.Import
{
    public class AirportTypeConverter : ITypeConverter
    {
        public object ConvertFromString(string text, IReaderRow row, MemberMapData memberMapData)
        {
            return text?.Trim().ToLowerInvariant() switch
            {
                "arrivals" => AirportType.Arrival,
                "departures" => AirportType.Departure,
                _ => AirportType.Unknown
            };
        }

        public string ConvertToString(object value, IWriterRow row, MemberMapData memberMapData)
        {
            return value switch
            {
                AirportType.Arrival => "Arrivals",
                AirportType.Departure => "Departures",
                _ => "Unknown"
            };
        }
    }
}
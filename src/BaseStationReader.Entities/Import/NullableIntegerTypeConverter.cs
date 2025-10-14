using System.Diagnostics.CodeAnalysis;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;

namespace BaseStationReader.Entities.Import
{
    [ExcludeFromCodeCoverage]
    public class NullableIntegerTypeConverter : BooleanConverter
    {
        public override object ConvertFromString(string text, IReaderRow row, MemberMapData memberMapData)
        {
            if (!int.TryParse(text, out int value))
            {
                return null;
            }

            return value;
        }
    }
}

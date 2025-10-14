using System.Diagnostics.CodeAnalysis;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;

namespace BaseStationReader.Entities.Import
{
    [ExcludeFromCodeCoverage]
    public class YesNoBooleanConverter : BooleanConverter
    {
        public override object ConvertFromString(string text, IReaderRow row, MemberMapData memberMapData)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            switch (text.Trim().ToLowerInvariant())
            {
                case "y":
                case "yes":
                case "true":
                    return true;
                case "n":
                case "no":
                case "false":
                    return false;
                default:
                    return base.ConvertFromString(text, row, memberMapData);
            }
        }
    }
}

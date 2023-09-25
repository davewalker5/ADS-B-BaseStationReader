using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace BaseStationReader.Entities.Config
{
    [ExcludeFromCodeCoverage]
    public class TrackerColumn
    {
        public string Property { get; set; } = "";
        public string Label { get; set; } = "";
        public string Format { get; set; } = "";
        public string Context { get; set; } = "";
        public PropertyInfo? Info { get; set; } = null;
        public string TypeName { get; set; } = "";
    }
}

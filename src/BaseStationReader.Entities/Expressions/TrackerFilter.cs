namespace BaseStationReader.Entities.Expressions
{
    public class TrackerFilter
    {
        public string PropertyName { get; set; } = "";
        public TrackerFilterOperator Operator { get; set; }
        public object? Value { get; set; } = null;
    }
}

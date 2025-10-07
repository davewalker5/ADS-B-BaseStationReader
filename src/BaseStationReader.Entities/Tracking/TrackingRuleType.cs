namespace BaseStationReader.Entities.Tracking
{
    [Flags]
    public enum TrackingRuleType
    {
        Any = 0x0,
        Descending = 0x01,
        Ascending = 0x02
    }
}
namespace BaseStationReader.Entities.Tracking
{
    public class TrackingRule
    {
        public double ReceiverLatitude { get; set; }
        public double ReceiverLongitude { get; set; }
        public decimal MaximumDistance { get; set; }
        public decimal MinimumDistance { get; set; }
        public decimal MaximumAltitude { get; set; }
        public decimal MinimumAltitude { get; set; }
        public TrackingRuleType RuleType { get; set; }
    }
}
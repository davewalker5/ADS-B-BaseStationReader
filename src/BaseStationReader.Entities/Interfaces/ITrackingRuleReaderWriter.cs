using BaseStationReader.Entities.Tracking;

namespace BaseStationReader.Entities.Interfaces
{
    public interface ITrackingRuleReaderWriter
    {
        IEnumerable<TrackingRule> Read(string filePath);
        void Write(IEnumerable<TrackingRule> rules, string filePath);
    }
}
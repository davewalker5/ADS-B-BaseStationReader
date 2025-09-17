using System.Text.Json;
using BaseStationReader.Entities.Interfaces;
using BaseStationReader.Entities.Tracking;

namespace BaseStationReader.BusinessLogic.Tracking
{
    public class TrackingRuleReaderWriter : ITrackingRuleReaderWriter
    {
        private readonly JsonSerializerOptions options = new()
        {
            WriteIndented = true
        };

        public IEnumerable<TrackingRule> Read(string filePath)
        {
            var json = File.ReadAllText(filePath);
            var rules = JsonSerializer.Deserialize<List<TrackingRule>>(json);
            return rules;
        }

        public void Write(IEnumerable<TrackingRule> rules, string filePath)
        {
            var json = JsonSerializer.Serialize(rules, options);
            File.WriteAllText(filePath, json);
        }
    }
}

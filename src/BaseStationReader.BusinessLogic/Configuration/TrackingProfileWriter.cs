using System.Text.Json;
using BaseStationReader.Entities.Config;
using BaseStationReader.Entities.Interfaces;

namespace BaseStationReader.BusinessLogic.Configuration
{
    public class TrackingProfileReaderWriter : ITrackingProfileReaderWriter
    {
        private readonly JsonSerializerOptions SerializerOptions = new()
        {
            WriteIndented = true
        };

        /// <summary>
        /// Read a tracking profile from a JSON file
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public TrackingProfile Read(string filePath)
        {
            var json = File.ReadAllText(filePath);
            var profile = JsonSerializer.Deserialize<TrackingProfile>(json);
            return profile;
        }

        /// <summary>
        /// Write a tracking profile to a JSON file
        /// </summary>
        /// <param name="profile"></param>
        /// <param name="filePath"></param>
        public void Write(TrackingProfile profile, string filePath)
        {
            var json = JsonSerializer.Serialize(profile, SerializerOptions);
            File.WriteAllText(filePath, json);
        }
    }
}
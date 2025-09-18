using System.Text.Json;
using System.Text.Json.Serialization;
using BaseStationReader.Entities.Config;
using BaseStationReader.Entities.Interfaces;

namespace BaseStationReader.BusinessLogic.Configuration
{
    public class TrackingProfileReaderWriter : ITrackingProfileReaderWriter
    {
        private readonly JsonSerializerOptions _serializerOptions;

        public TrackingProfileReaderWriter()
        {
            // Configure the JSON serialization options so Enums are written as member names, not
            // the corresponding integer values
            _serializerOptions = new() { WriteIndented = true };
            _serializerOptions.Converters.Add(new JsonStringEnumConverter());
        }

        /// <summary>
        /// Read a tracking profile from a JSON file
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public TrackingProfile Read(string filePath)
        {
            var json = File.ReadAllText(filePath);
            var profile = JsonSerializer.Deserialize<TrackingProfile>(json, _serializerOptions);
            return profile;
        }

        /// <summary>
        /// Write a tracking profile to a JSON file
        /// </summary>
        /// <param name="profile"></param>
        /// <param name="filePath"></param>
        public void Write(TrackingProfile profile, string filePath)
        {
            var json = JsonSerializer.Serialize(profile, _serializerOptions);
            File.WriteAllText(filePath, json);
        }
    }
}
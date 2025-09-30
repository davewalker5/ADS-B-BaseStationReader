using BaseStationReader.Entities.Config;

namespace BaseStationReader.Interfaces.Config
{
    public interface ITrackingProfileReaderWriter
    {
        TrackingProfile Read(string filePath);
        void Write(TrackingProfile profile, string filePath);
    }
}
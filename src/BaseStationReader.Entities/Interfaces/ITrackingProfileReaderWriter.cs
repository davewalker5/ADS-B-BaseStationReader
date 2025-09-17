using BaseStationReader.Entities.Config;

namespace BaseStationReader.Entities.Interfaces
{
    public interface ITrackingProfileReaderWriter
    {
        TrackingProfile Read(string filePath);
        void Write(TrackingProfile profile, string filePath);
    }
}
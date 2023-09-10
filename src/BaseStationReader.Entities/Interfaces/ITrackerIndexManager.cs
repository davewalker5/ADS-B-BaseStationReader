namespace BaseStationReader.Entities.Interfaces
{
    public interface ITrackerIndexManager
    {
        void AddAircraft(string address, int rowNumber);
        int FindAircraft(string address);
        int RemoveAircraft(string address);
    }
}
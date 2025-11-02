using BaseStationReader.Entities.Tracking;
using Spectre.Console;

namespace BaseStationReader.Terminal.Interfaces
{
    internal interface ITrackerTableManager
    {
        Table Table { get; }

        void CreateTable(string title);
        int AddOrUpdateAircraft(TrackedAircraft aircraft);
        int RemoveAircraft(TrackedAircraft aircraft);
    }
}
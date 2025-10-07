using BaseStationReader.Entities.Tracking;
using Spectre.Console;

namespace BaseStationReader.Terminal.Interfaces
{
    internal interface ITrackerTableManager
    {
        Table Table { get; }

        int AddAircraft(TrackedAircraft aircraft);
        void CreateTable(string title);
        int RemoveAircraft(TrackedAircraft aircraft);
        int UpdateAircraft(TrackedAircraft aircraft);
    }
}
using BaseStationReader.Entities.Tracking;
using Spectre.Console;

namespace BaseStationReader.Terminal.Interfaces
{
    internal interface ITrackerTableManager
    {
        Table? Table { get; }

        int AddAircraft(Aircraft aircraft);
        void CreateTable(string title);
        int RemoveAircraft(Aircraft aircraft);
        int UpdateAircraft(Aircraft aircraft);
    }
}
using BaseStationReader.Entities.Tracking;
using BaseStationReader.TrackerHub.Interfaces;

namespace BaseStationReader.BusinessLogic.TrackerHub.Logic
{
    public class AircraftState : IAircraftState
    {
        private readonly Dictionary<string, TrackedAircraft> _aircraft = new(StringComparer.OrdinalIgnoreCase);
        private readonly object _gate = new();

        public IReadOnlyCollection<TrackedAircraft> All()
        {
            lock (_gate) return _aircraft.Values.ToArray();
        }

        public void Upsert(TrackedAircraft address)
        {
            lock (_gate)
            {
                _aircraft[address.Address] = address;
            }
        }

        public void Remove(string icao, DateTimeOffset whenUtc)
        {
            lock (_gate)
            {
                _aircraft.Remove(icao);
            }
        }
    }
}
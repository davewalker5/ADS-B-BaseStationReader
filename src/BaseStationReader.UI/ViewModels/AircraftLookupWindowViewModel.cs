using BaseStationReader.Entities.Config;
using BaseStationReader.Entities.Interfaces;
using BaseStationReader.Entities.Lookup;
using BaseStationReader.UI.Models;
using ReactiveUI;
using System.Reactive;

namespace BaseStationReader.UI.ViewModels
{
    public class AircraftLookupWindowViewModel : AircraftLookupCriteria
    {
        private readonly AircraftLookupModel _aircraftLookup;

        public ReactiveCommand<Unit, AircraftLookupCriteria?> CloseCommand { get; private set; }

        public AircraftLookupWindowViewModel(ITrackerLogger logger, TrackerApplicationSettings settings, AircraftLookupCriteria? initialValues)
        {
            // Set up the aircraft lookup model
            _aircraftLookup = new AircraftLookupModel(logger, settings);

            // Populate from the initial values, if supplied
            Address = initialValues?.Address;

            // Create a command that can be bound to the Cancel button on the dialog
            CloseCommand = ReactiveCommand.Create(() => { return (AircraftLookupCriteria?)this; });
        }

        /// <summary>
        /// Search for the specified aircraft
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public AircraftDetails? LookupAircraft(string? address)
            => _aircraftLookup.LookupAircraft(address);

        /// <summary>
        /// Search for an active flight for the specified aircraft
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public FlightDetails? LookupActiveFlight(string? address)
            => _aircraftLookup.LookupActiveFlight(address);
    }
}

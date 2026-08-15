using BaseStationReader.Interfaces.Logging;
using BaseStationReader.Interfaces.Tracking;

namespace BaseStationReader.Interfaces.Database
{
    public interface IDatabaseManagementFactory
    {
        T Context<T>() where T : class;
        ITrackerLogger Logger { get; }
        IDataCleaner DataCleaner { get; }
        IAircraftManager AircraftManager { get; }
        IAirlineManager AirlineManager { get; }
        IAirportManager AirportManager { get; }
        IFlightManager FlightManager { get; }
        IManufacturerManager ManufacturerManager { get; }
        IModelManager ModelManager { get; }
        ISightingManager SightingManager { get; }
        ITrackedAircraftWriter TrackedAircraftWriter { get; }
        IPositionWriter PositionWriter { get; }
        IAircraftLifetimeManager AircraftLifetimeManager { get; }
        IExcludedAddressManager ExcludedAddressManager { get; }
        IExcludedCallsignManager ExcludedCallsignManager { get; }
        IApiLogManager ApiLogManager { get; }
        IProvenanceManager ProvenanceManager { get; }
        IObservationSessionManager ObservationSessionManager { get; }
        IPositionDensitySnapshotManager PositionDensitySnapshotManager { get; }
        IEquipmentTypeManager EquipmentTypeManager { get; }
        IEquipmentManager EquipmentManager { get; }
        ISessionEquipmentManager SessionEquipmentManager { get; }
        IAircraftNoteManager AircraftNoteManager { get; }
    }
}

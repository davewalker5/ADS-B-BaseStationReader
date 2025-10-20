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
        IFlightManager FlightManager { get; }
        IManufacturerManager ManufacturerManager { get; }
        IModelManager ModelManager { get; }
        ISightingManager SightingManager { get; }
        IFlightIATACodeMappingManager FlightIATACodeMappingManager { get; }
        ITrackedAircraftWriter TrackedAircraftWriter { get; }
        IPositionWriter PositionWriter { get; }
        IAircraftLockManager AircraftLockManager { get; }
        IExcludedAddressManager ExcludedAddressManager { get; }
        IExcludedCallsignManager ExcludedCallsignManager { get; }
        IApiLogManager ApiLogManager { get; }
    }
}
namespace BaseStationReader.Interfaces.Database
{
    public interface IDatabaseManagementFactory
    {
        T Context<T>() where T : class;
        IAircraftManager AircraftManager { get; }
        IAirlineManager AirlineManager { get; }
        IFlightManager FlightManager { get; }
        IManufacturerManager ManufacturerManager { get; }
        IModelManager ModelManager { get; }
        ISightingManager SightingManager { get; }
        IConfirmedMappingManager ConfirmedMappingManager { get; }
    }
}
using BaseStationReader.Data;
using BaseStationReader.Interfaces.Database;

namespace BaseStationReader.BusinessLogic.Database
{
    public class DatabaseManagementFactory : IDatabaseManagementFactory
    {
        private readonly BaseStationReaderDbContext _context;
        private readonly Lazy<IAircraftManager> _aircraftManager = null;
        private readonly Lazy<IAirlineManager> _airlineManager = null;
        private readonly Lazy<IFlightManager> _flightManager = null;
        private readonly Lazy<IManufacturerManager> _manufacturerManager = null;
        private readonly Lazy<IModelManager> _modelManager = null;
        private readonly Lazy<ISightingManager> _sightingManager = null;

        public IAircraftManager AircraftManager { get { return _aircraftManager.Value; } }

        public IAirlineManager AirlineManager { get { return _airlineManager.Value; } }

        public IFlightManager FlightManager { get { return _flightManager.Value; } }

        public IManufacturerManager ManufacturerManager { get { return _manufacturerManager.Value; } }

        public IModelManager ModelManager { get { return _modelManager.Value; } }

        public ISightingManager SightingManager { get { return _sightingManager.Value; } }

        public DatabaseManagementFactory(BaseStationReaderDbContext context)
        {
            _context = context;

            _aircraftManager = new Lazy<IAircraftManager>(() => new AircraftManager(context));
            _airlineManager = new Lazy<IAirlineManager>(() => new AirlineManager(context));
            _flightManager = new Lazy<IFlightManager>(() => new FlightManager(context));
            _manufacturerManager = new Lazy<IManufacturerManager>(() => new ManufacturerManager(context));
            _modelManager = new Lazy<IModelManager>(() => new ModelManager(context));
            _sightingManager = new Lazy<ISightingManager>(() => new SightingManager(context));
        }

        /// <summary>
        /// Convenience method to retrieve the context as the specified type "T". This allows the interfaces
        /// assembly, that defines the interface for this class, to be independent of the data assembly where
        /// the context is defined
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T Context<T>() where T : class
            => _context as T;
    }
}
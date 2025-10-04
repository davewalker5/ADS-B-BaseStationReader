using BaseStationReader.BusinessLogic.Configuration;
using BaseStationReader.Data;
using BaseStationReader.Entities.Config;
using BaseStationReader.Interfaces.Database;
using BaseStationReader.Interfaces.Logging;

namespace BaseStationReader.Lookup.Logic
{
    internal abstract class CommandHandlerBase
    {
        protected LookupToolApplicationSettings Settings { get; private set; }
        protected LookupToolCommandLineParser Parser { get; private set; }
        protected ITrackerLogger Logger { get; private set; }
        protected IDatabaseManagementFactory Factory { get; private set; }
        protected BaseStationReaderDbContext Context { get { return Factory.Context<BaseStationReaderDbContext>(); }}

        public CommandHandlerBase(
            LookupToolApplicationSettings settings,
            LookupToolCommandLineParser parser,
            ITrackerLogger logger,
            IDatabaseManagementFactory factory)
        {
            Settings = settings;
            Parser = parser;
            Logger = logger;
            Factory = factory;
        }
    }
}